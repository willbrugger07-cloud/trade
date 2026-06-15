'use strict';
/**
 * LEDGER WALLET SERVICE
 * Append-only double-entry ledger. Handles all balance mutations atomically.
 * No game logic here — this service only knows about money.
 *
 * In production: swap in-memory store for PostgreSQL with
 *   BEGIN / SELECT balance FROM accounts WHERE id=$1 FOR UPDATE
 *   INSERT INTO ledger_events ...
 *   UPDATE accounts SET balance=...
 *   COMMIT
 *
 * Inter-service: expose HTTP on LEDGER_PORT (default 3002).
 * In production replace HTTP with NATS/Kafka message bus.
 */

const http   = require('http');
const crypto = require('crypto');

const PORT = process.env.LEDGER_PORT || 3002;

// ─── IN-MEMORY STORES (replace with DB) ──────────────────────────────────────
// accounts: accountId → { balance: number, status: 'IDLE'|'LOCKED' }
const accounts = new Map();

// ledger: append-only array of events — NEVER mutated, only pushed
// Each event: { id, accountId, roundId, type, amount, balanceBefore, balanceAfter, ts }
const ledger = [];

// round registry: roundId → { accountId, wager, status, createdAt }
const rounds = new Map();

// ─── ACCOUNT HELPERS ─────────────────────────────────────────────────────────
function getAccount(accountId) {
  if (!accounts.has(accountId)) {
    accounts.set(accountId, { balance: 1000.00, status: 'IDLE' });
  }
  return accounts.get(accountId);
}

function appendEvent(entry) {
  const event = { id: crypto.randomUUID(), ts: Date.now(), ...entry };
  ledger.push(event);
  return event;
}

// ─── ATOMIC WAGER DEBIT ───────────────────────────────────────────────────────
// Returns { ok, roundId, balanceAfter, error }
function debitWager(accountId, wager) {
  const acct = getAccount(accountId);

  // Re-entrancy guard
  if (acct.status === 'LOCKED') {
    return { ok: false, error: 'ACCOUNT_LOCKED', code: 429 };
  }
  if (acct.balance < wager) {
    return { ok: false, error: 'INSUFFICIENT_BALANCE', code: 402 };
  }

  // Atomic: lock → debit → register round
  acct.status = 'LOCKED';
  const balanceBefore  = acct.balance;
  acct.balance         = round2(acct.balance - wager);
  const roundId        = crypto.randomUUID();

  rounds.set(roundId, { accountId, wager, status: 'IN_PROGRESS', createdAt: Date.now() });

  appendEvent({
    accountId, roundId,
    type:          'WAGER_DEBIT',
    amount:        -wager,
    balanceBefore,
    balanceAfter:  acct.balance,
  });

  return { ok: true, roundId, balanceAfter: acct.balance };
}

// ─── PAYOUT CREDIT ────────────────────────────────────────────────────────────
// Returns { ok, balanceAfter, error }
function creditPayout(roundId, payout, manifestSummary = {}) {
  const round = rounds.get(roundId);
  if (!round) return { ok: false, error: 'ROUND_NOT_FOUND', code: 404 };
  if (round.status !== 'IN_PROGRESS') return { ok: false, error: 'ROUND_ALREADY_SETTLED', code: 409 };

  const acct        = getAccount(round.accountId);
  const balanceBefore = acct.balance;
  acct.balance      = round2(acct.balance + payout);
  acct.status       = 'IDLE';
  round.status      = 'SETTLED';
  round.settledAt   = Date.now();

  appendEvent({
    accountId:    round.accountId,
    roundId,
    type:         'PAYOUT_CREDIT',
    amount:       payout,
    balanceBefore,
    balanceAfter: acct.balance,
    meta:         manifestSummary,   // stores ticket_id, cascades, final_volt_mult
  });

  // Net P&L event for audit trail
  const net = round2(payout - round.wager);
  appendEvent({
    accountId:    round.accountId,
    roundId,
    type:         net >= 0 ? 'NET_WIN' : 'NET_LOSS',
    amount:       net,
    balanceBefore: acct.balance,
    balanceAfter:  acct.balance,
  });

  return { ok: true, balanceAfter: acct.balance };
}

// ─── ROLLBACK (on math engine error) ─────────────────────────────────────────
function rollbackRound(roundId, reason = 'ENGINE_ERROR') {
  const round = rounds.get(roundId);
  if (!round || round.status !== 'IN_PROGRESS') return { ok: false };
  const acct = getAccount(round.accountId);
  const balanceBefore = acct.balance;
  acct.balance  = round2(acct.balance + round.wager);
  acct.status   = 'IDLE';
  round.status  = 'ROLLED_BACK';
  appendEvent({
    accountId: round.accountId, roundId,
    type: 'ROLLBACK', amount: round.wager,
    balanceBefore, balanceAfter: acct.balance,
    meta: { reason },
  });
  return { ok: true, balanceAfter: acct.balance };
}

// ─── LEDGER REPLAY (audit: re-derive balance from scratch) ───────────────────
function replayBalance(accountId) {
  let balance = 1000.00; // starting balance
  for (const e of ledger) {
    if (e.accountId === accountId) balance = round2(balance + e.amount);
  }
  return balance;
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

const server = http.createServer(async (req, res) => {
  const url = req.url, method = req.method;

  // GET /account/:id
  if (method === 'GET' && url.startsWith('/account/')) {
    const accountId = url.split('/')[2];
    const acct = getAccount(accountId);
    return send(res, 200, { accountId, balance: acct.balance, status: acct.status });
  }

  // POST /debit  { accountId, wager }
  if (method === 'POST' && url === '/debit') {
    const { accountId, wager } = await readBody(req);
    const result = debitWager(accountId, wager);
    return send(res, result.ok ? 200 : (result.code || 400), result);
  }

  // POST /credit  { roundId, payout, manifest }
  if (method === 'POST' && url === '/credit') {
    const { roundId, payout, manifest } = await readBody(req);
    const result = creditPayout(roundId, payout, manifest);
    return send(res, result.ok ? 200 : (result.code || 400), result);
  }

  // POST /rollback  { roundId, reason }
  if (method === 'POST' && url === '/rollback') {
    const { roundId, reason } = await readBody(req);
    return send(res, 200, rollbackRound(roundId, reason));
  }

  // GET /ledger/:accountId  — full audit trail
  if (method === 'GET' && url.startsWith('/ledger/')) {
    const accountId = url.split('/')[2];
    const events = ledger.filter(e => e.accountId === accountId);
    const replayed = replayBalance(accountId);
    return send(res, 200, { accountId, events, replayed_balance: replayed, event_count: events.length });
  }

  // GET /round/:roundId
  if (method === 'GET' && url.startsWith('/round/')) {
    const roundId = url.split('/')[2];
    const round = rounds.get(roundId);
    return send(res, round ? 200 : 404, round || { error: 'not found' });
  }

  send(res, 404, { error: 'not found' });
});

server.listen(PORT, () => console.log(`[Ledger]  http://localhost:${PORT}`));

module.exports = { debitWager, creditPayout, rollbackRound, replayBalance };

function round2(n) { return Math.round(n * 100) / 100; }
