'use strict';
/**
 * LEDGER WALLET SERVICE v2 — Event Sourced
 *
 * Architecture:
 *   Append-Only Event Log  →  Redis-style Materialized Balance Cache
 *
 * Rules:
 *   - NEVER UPDATE a financial row. Only INSERT new events.
 *   - Balance = fast read from cache (O(1))
 *   - Cache can always be rebuilt by replaying the event log (audit-proof)
 *   - Atomic stored-procedure pattern:
 *       BEGIN
 *         INSERT BET_DEBIT (PENDING)
 *         CHECK balance >= wager  (abort if not)
 *         INSERT WIN_CREDIT
 *         UPDATE cache
 *       COMMIT
 *
 * In production: replace in-memory stores with:
 *   eventLog  → PostgreSQL append-only table (no UPDATE/DELETE permissions granted)
 *   cache     → Redis HSET/HGET with WATCH+MULTI for optimistic locking
 *   roundMap  → PostgreSQL rounds table
 */

const http   = require('http');
const crypto = require('crypto');

const PORT = process.env.LEDGER_PORT || 3002;

// ─── EVENT TYPES ─────────────────────────────────────────────────────────────
const EVT = {
  ACCOUNT_CREATED:  'ACCOUNT_CREATED',
  BET_DEBIT:        'BET_DEBIT',
  WIN_CREDIT:       'WIN_CREDIT',
  NET_WIN:          'NET_WIN',
  NET_LOSS:         'NET_LOSS',
  ROLLBACK:         'ROLLBACK',
  DEPOSIT_CREDIT:   'DEPOSIT_CREDIT',
  WITHDRAWAL_DEBIT: 'WITHDRAWAL_DEBIT',
};

// ─── APPEND-ONLY EVENT LOG ───────────────────────────────────────────────────
// In production: INSERT INTO ledger_events (id, account_id, round_id, type, amount, meta, created_at)
// GRANT INSERT ON ledger_events TO app_user;
// REVOKE UPDATE, DELETE ON ledger_events FROM app_user;  ← enforced at DB level
const eventLog = [];

function appendEvent(fields) {
  const event = { id: crypto.randomUUID(), ts: Date.now(), ...fields };
  eventLog.push(Object.freeze(event)); // freeze: immutable once written
  return event;
}

// ─── MATERIALIZED BALANCE CACHE (Redis-style) ─────────────────────────────────
// In production: Redis HSET accounts:{id} balance {n} status {s}
// Read: Redis HGET accounts:{id} balance   → O(1), never touches event log
const balanceCache = new Map(); // accountId → { balance, status, version }

function getCached(accountId) {
  if (!balanceCache.has(accountId)) {
    balanceCache.set(accountId, { balance: 1000.00, status: 'IDLE', version: 0 });
    appendEvent({ accountId, type: EVT.ACCOUNT_CREATED, amount: 1000.00,
                  balanceBefore: 0, balanceAfter: 1000.00 });
  }
  return balanceCache.get(accountId);
}

function updateCache(accountId, patch) {
  const current = getCached(accountId);
  const updated = { ...current, ...patch, version: current.version + 1 };
  balanceCache.set(accountId, updated);
  return updated;
}

// ─── ROUND REGISTRY ───────────────────────────────────────────────────────────
const rounds = new Map(); // roundId → { accountId, wager, status, createdAt, settledAt }

// ─── ATOMIC SPIN TRANSACTION ─────────────────────────────────────────────────
/**
 * Mirrors a database stored procedure:
 *   BEGIN
 *     INSERT BET_DEBIT PENDING
 *     CHECK balance
 *     RESERVE round
 *   COMMIT
 */
function debitWager(accountId, wager) {
  const cache = getCached(accountId);

  if (cache.status === 'LOCKED') {
    return { ok: false, error: 'ACCOUNT_LOCKED', code: 429 };
  }
  if (cache.balance < wager) {
    return { ok: false, error: 'INSUFFICIENT_BALANCE', code: 402 };
  }

  // Atomic block start — in production: BEGIN TRANSACTION + SELECT FOR UPDATE
  const balanceBefore = cache.balance;
  const newBalance    = r2(cache.balance - wager);
  const roundId       = crypto.randomUUID();

  // 1. INSERT BET_DEBIT event
  appendEvent({
    accountId, roundId, type: EVT.BET_DEBIT,
    amount: -wager, balanceBefore, balanceAfter: newBalance,
  });

  // 2. UPDATE cache (Redis: HSET accounts:{id} balance {n} status LOCKED)
  updateCache(accountId, { balance: newBalance, status: 'LOCKED' });

  // 3. Register round
  rounds.set(roundId, { accountId, wager, status: 'IN_PROGRESS', createdAt: Date.now() });

  // Atomic block end — in production: COMMIT
  return { ok: true, roundId, balanceAfter: newBalance };
}

/**
 *   BEGIN
 *     INSERT WIN_CREDIT
 *     INSERT NET_WIN or NET_LOSS
 *     UPDATE cache (release lock)
 *   COMMIT
 */
function creditPayout(roundId, payout, meta = {}) {
  const round = rounds.get(roundId);
  if (!round) return { ok: false, error: 'ROUND_NOT_FOUND', code: 404 };
  if (round.status !== 'IN_PROGRESS') return { ok: false, error: 'ROUND_ALREADY_SETTLED', code: 409 };

  const cache         = getCached(round.accountId);
  const balanceBefore = cache.balance;
  const newBalance    = r2(cache.balance + payout);
  const net           = r2(payout - round.wager);

  // INSERT WIN_CREDIT
  appendEvent({
    accountId: round.accountId, roundId, type: EVT.WIN_CREDIT,
    amount: payout, balanceBefore, balanceAfter: newBalance, meta,
  });

  // INSERT NET event
  appendEvent({
    accountId: round.accountId, roundId,
    type:   net >= 0 ? EVT.NET_WIN : EVT.NET_LOSS,
    amount: net, balanceBefore: newBalance, balanceAfter: newBalance,
  });

  // UPDATE cache — release lock
  updateCache(round.accountId, { balance: newBalance, status: 'IDLE' });
  round.status    = 'SETTLED';
  round.settledAt = Date.now();

  return { ok: true, balanceAfter: newBalance };
}

function rollbackRound(roundId, reason = 'ENGINE_ERROR') {
  const round = rounds.get(roundId);
  if (!round || round.status !== 'IN_PROGRESS') return { ok: false, error: 'NOTHING_TO_ROLLBACK' };

  const cache         = getCached(round.accountId);
  const balanceBefore = cache.balance;
  const newBalance    = r2(cache.balance + round.wager);

  appendEvent({
    accountId: round.accountId, roundId, type: EVT.ROLLBACK,
    amount: round.wager, balanceBefore, balanceAfter: newBalance, meta: { reason },
  });

  updateCache(round.accountId, { balance: newBalance, status: 'IDLE' });
  round.status = 'ROLLED_BACK';
  return { ok: true, balanceAfter: newBalance };
}

// External credit (deposit confirmed by wallet sync)
function creditDeposit(accountId, amount, txHash) {
  const cache = getCached(accountId);
  const newBalance = r2(cache.balance + amount);
  appendEvent({ accountId, type: EVT.DEPOSIT_CREDIT, amount,
                balanceBefore: cache.balance, balanceAfter: newBalance, meta: { txHash } });
  updateCache(accountId, { balance: newBalance });
  return { ok: true, balanceAfter: newBalance };
}

// ─── AUDIT REPLAY ─────────────────────────────────────────────────────────────
// Re-derive balance from raw events — proves cache matches log
function replayBalance(accountId) {
  let balance = 0;
  for (const e of eventLog) {
    if (e.accountId === accountId) balance = r2(balance + e.amount);
  }
  return balance;
}

function auditAccount(accountId) {
  const events   = eventLog.filter(e => e.accountId === accountId);
  const replayed = replayBalance(accountId);
  const cached   = getCached(accountId).balance;
  return {
    accountId, events, event_count: events.length,
    replayed_balance: replayed,
    cached_balance:   cached,
    cache_matches:    Math.abs(replayed - cached) < 0.001,
  };
}

// ─── HTTP SERVER ──────────────────────────────────────────────────────────────
function readBody(req) {
  return new Promise((res, rej) => {
    let d = '';
    req.on('data', c => d += c);
    req.on('end', () => { try { res(JSON.parse(d || '{}')); } catch(e) { rej(e); } });
  });
}
function send(res, status, body) {
  res.writeHead(status, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify(body));
}

http.createServer(async (req, res) => {
  const url = req.url, method = req.method;

  if (method === 'GET' && url.startsWith('/account/')) {
    const accountId = url.split('/')[2];
    const c = getCached(accountId);
    return send(res, 200, { accountId, balance: c.balance, status: c.status, version: c.version });
  }

  if (method === 'POST' && url === '/debit') {
    const { accountId, wager } = await readBody(req);
    return send(res, 200, debitWager(accountId, wager));
  }

  if (method === 'POST' && url === '/credit') {
    const { roundId, payout, manifest } = await readBody(req);
    const result = creditPayout(roundId, payout, manifest);
    return send(res, result.ok ? 200 : (result.code || 400), result);
  }

  if (method === 'POST' && url === '/rollback') {
    const { roundId, reason } = await readBody(req);
    return send(res, 200, rollbackRound(roundId, reason));
  }

  if (method === 'POST' && url === '/credit-deposit') {
    const { accountId, amount, txHash } = await readBody(req);
    return send(res, 200, creditDeposit(accountId, amount, txHash));
  }

  if (method === 'GET' && url.startsWith('/ledger/')) {
    return send(res, 200, auditAccount(url.split('/')[2]));
  }

  if (method === 'GET' && url.startsWith('/round/')) {
    const r = rounds.get(url.split('/')[2]);
    return send(res, r ? 200 : 404, r || { error: 'not found' });
  }

  if (url === '/health') return send(res, 200, { status: 'ok', service: 'ledger-v2' });

  send(res, 404, { error: 'not found' });
}).listen(PORT, () => console.log(`[Ledger]  http://localhost:${PORT}  (event-sourced)`));

function r2(n) { return Math.round(n * 100) / 100; }

module.exports = { debitWager, creditPayout, rollbackRound, replayBalance, auditAccount };
