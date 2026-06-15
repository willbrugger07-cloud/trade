'use strict';
/**
 * WALLET SYNC SERVICE
 * Three-tier wallet architecture + blockchain settlement layer.
 *
 * TIER 1 — Hot Wallet  (operational):  connected to game server, handles live bets/withdrawals
 * TIER 2 — Warm Wallet (buffer):       auto-sweep if hot wallet exceeds threshold
 * TIER 3 — Cold Wallet (treasury):     offline vault, 90%+ of platform capital, manual only
 *
 * The game server (Ledger Service) NEVER talks to the blockchain directly.
 * It only mutates the internal SQL/in-memory ledger at game speed.
 * This service runs on a separate process and batch-settles to chain on schedule.
 *
 * In production swap placeholder blockchain calls with:
 *   - ethers.js / web3.js for EVM chains (ETH, BNB, MATIC)
 *   - @solana/web3.js for Solana
 *   - bitcoinjs-lib for BTC
 *
 * Run: SYNC_PORT=3003 node services/wallet-sync-service.js
 */

const http   = require('http');
const crypto = require('crypto');

const PORT         = process.env.SYNC_PORT   || 3003;
const LEDGER_PORT  = process.env.LEDGER_PORT || 3002;

// ─── WALLET TIER CONFIG ───────────────────────────────────────────────────────
const WALLET_CONFIG = {
  hot: {
    address:          process.env.HOT_WALLET_ADDRESS  || '0xHOT_WALLET_ADDRESS',
    sweepThreshold:   10_000,   // USD — sweep to warm if hot balance > this
    minReserve:       1_000,    // USD — keep at least this in hot wallet
    label:            'HOT',
  },
  warm: {
    address:          process.env.WARM_WALLET_ADDRESS || '0xWARM_WALLET_ADDRESS',
    sweepThreshold:   100_000,  // USD — sweep excess to cold if warm > this
    label:            'WARM',
  },
  cold: {
    address:          process.env.COLD_WALLET_ADDRESS || '0xCOLD_WALLET_ADDRESS',
    label:            'COLD',
    // COLD wallet is read-only from this service. Outbound tx require manual signing.
  },
};

// Batch settlement interval (ms). 60s default — tune per chain's block time.
const SETTLEMENT_INTERVAL_MS = parseInt(process.env.SETTLEMENT_INTERVAL_MS) || 60_000;

// ─── IN-MEMORY STATE (replace with DB tables) ─────────────────────────────────
// pending withdrawals: { id, accountId, toAddress, amount, currency, status, createdAt }
const withdrawalQueue = [];
// pending deposits: { id, txHash, fromAddress, amount, currency, confirmations, status }
const depositQueue    = [];
// wallet balances (simulated; in production read from chain)
const walletBalances  = { hot: 50_000, warm: 200_000, cold: 2_000_000 };
// known bad addresses (AML screening list — in production use Chainalysis/Elliptic API)
const BLACKLIST = new Set([
  '0xBAD_MIXER_ADDRESS_1',
  '0xBAD_MIXER_ADDRESS_2',
  '0xKNOWN_HACKER_ADDRESS',
]);
// flagged accounts (frozen pending review)
const flaggedAccounts = new Map(); // accountId → { reason, flaggedAt, txHashes }

// Settlement batch log
const settlementLog = [];

// ─── AML SCREENING ────────────────────────────────────────────────────────────
function screenAddress(address, accountId, amount) {
  const issues = [];

  if (BLACKLIST.has(address.toLowerCase())) {
    issues.push('ADDRESS_BLACKLISTED');
  }
  // Large transaction threshold ($10k+ triggers enhanced review)
  if (amount >= 10_000) {
    issues.push('LARGE_TRANSACTION_REVIEW');
  }
  // In production: call Chainalysis/Elliptic API here
  // const risk = await chainalysis.screen(address);
  // if (risk.score > 7) issues.push('HIGH_RISK_SCORE');

  if (issues.length) {
    const existing = flaggedAccounts.get(accountId) || { reason: [], flaggedAt: Date.now(), txHashes: [] };
    existing.reason.push(...issues);
    flaggedAccounts.set(accountId, existing);
    console.warn(`[AML] Account ${accountId} flagged: ${issues.join(', ')}`);
  }

  return { clean: issues.length === 0, issues };
}

// ─── BLOCKCHAIN ADAPTER (stub — replace with chain-specific SDK) ──────────────
// These functions simulate on-chain operations. Each returns a tx hash.
async function broadcastTx(from, to, amount, currency) {
  // In production: sign with hot wallet private key and broadcast
  // const wallet = new ethers.Wallet(process.env.HOT_WALLET_PK, provider);
  // const tx = await wallet.sendTransaction({ to, value: ethers.parseEther(amount.toString()) });
  // return tx.hash;
  const fakeTxHash = '0x' + crypto.randomBytes(32).toString('hex');
  console.log(`[Chain] TX ${fakeTxHash} | ${from} → ${to} | ${amount} ${currency}`);
  return fakeTxHash;
}

async function getConfirmations(txHash) {
  // In production: provider.getTransactionReceipt(txHash)
  return Math.floor(Math.random() * 10) + 1; // simulate 1-10 confirmations
}

async function getOnChainBalance(address) {
  // In production: provider.getBalance(address) or token.balanceOf(address)
  return walletBalances[
    Object.values(WALLET_CONFIG).find(w => w.address === address)?.label?.toLowerCase() || 'hot'
  ] || 0;
}

// ─── HOT → WARM SWEEP ─────────────────────────────────────────────────────────
async function checkAndSweepHotToWarm() {
  const hotBalance = walletBalances.hot;
  if (hotBalance <= WALLET_CONFIG.hot.sweepThreshold) return null;

  const sweepAmount = hotBalance - WALLET_CONFIG.hot.minReserve;
  console.log(`[Sweep] HOT→WARM: sweeping $${sweepAmount.toFixed(2)}`);

  const txHash = await broadcastTx(
    WALLET_CONFIG.hot.address,
    WALLET_CONFIG.warm.address,
    sweepAmount, 'USD'
  );
  walletBalances.hot  -= sweepAmount;
  walletBalances.warm += sweepAmount;

  settlementLog.push({
    id: crypto.randomUUID(), type: 'SWEEP_HOT_TO_WARM',
    amount: sweepAmount, txHash, ts: Date.now(),
  });
  return txHash;
}

async function checkAndSweepWarmToCold() {
  const warmBalance = walletBalances.warm;
  if (warmBalance <= WALLET_CONFIG.warm.sweepThreshold) return null;

  const sweepAmount = warmBalance - WALLET_CONFIG.warm.sweepThreshold * 0.5;
  console.log(`[Sweep] WARM→COLD: sweeping $${sweepAmount.toFixed(2)}`);

  // Cold wallet tx REQUIRES manual signing in production — log intent only
  const intentId = crypto.randomUUID();
  settlementLog.push({
    id: intentId, type: 'SWEEP_WARM_TO_COLD_INTENT',
    amount: sweepAmount, status: 'AWAITING_MANUAL_SIGN',
    coldAddress: WALLET_CONFIG.cold.address, ts: Date.now(),
  });
  console.warn(`[Cold] Manual signing required for $${sweepAmount.toFixed(2)} → COLD. Intent: ${intentId}`);
  return null; // never auto-broadcast to cold
}

// ─── WITHDRAWAL PROCESSOR ─────────────────────────────────────────────────────
async function processWithdrawals() {
  const pending = withdrawalQueue.filter(w => w.status === 'PENDING');
  if (!pending.length) return;

  console.log(`[Sync] Processing ${pending.length} pending withdrawal(s)...`);

  for (const w of pending) {
    // AML check
    const screen = screenAddress(w.toAddress, w.accountId, w.amount);
    if (!screen.clean) {
      w.status = 'FROZEN';
      w.freezeReason = screen.issues;
      console.warn(`[AML] Withdrawal ${w.id} frozen: ${screen.issues.join(', ')}`);
      continue;
    }
    // Check hot wallet has enough
    if (walletBalances.hot < w.amount + WALLET_CONFIG.hot.minReserve) {
      w.status = 'DEFERRED_INSUFFICIENT_HOT';
      console.warn(`[Hot] Insufficient hot wallet balance for withdrawal ${w.id}`);
      continue;
    }

    w.status = 'BROADCASTING';
    try {
      const txHash = await broadcastTx(WALLET_CONFIG.hot.address, w.toAddress, w.amount, w.currency);
      walletBalances.hot -= w.amount;
      w.status  = 'BROADCAST';
      w.txHash  = txHash;
      w.sentAt  = Date.now();
      settlementLog.push({ id: crypto.randomUUID(), type: 'WITHDRAWAL', ...w });
    } catch (e) {
      w.status = 'BROADCAST_FAILED';
      w.error  = e.message;
      console.error(`[Chain] Withdrawal broadcast failed: ${e.message}`);
    }
  }
}

// ─── DEPOSIT CONFIRMATION CHECKER ─────────────────────────────────────────────
async function checkDeposits() {
  const unconfirmed = depositQueue.filter(d => d.status === 'PENDING' || d.status === 'CONFIRMING');
  for (const dep of unconfirmed) {
    dep.confirmations = await getConfirmations(dep.txHash);
    dep.status = dep.confirmations >= 3 ? 'CONFIRMED' : 'CONFIRMING';

    if (dep.status === 'CONFIRMED' && !dep.credited) {
      dep.credited = true;
      // Credit the internal ledger
      await callLedger('/credit-deposit', { accountId: dep.accountId, amount: dep.amount, txHash: dep.txHash });
      walletBalances.hot += dep.amount;
      console.log(`[Deposit] Confirmed ${dep.txHash} — credited $${dep.amount} to ${dep.accountId}`);
      settlementLog.push({ id: crypto.randomUUID(), type: 'DEPOSIT_CONFIRMED', ...dep });
    }
  }
}

// ─── MAIN SETTLEMENT LOOP ─────────────────────────────────────────────────────
async function runSettlementCycle() {
  console.log(`[Sync] Settlement cycle at ${new Date().toISOString()}`);
  try {
    await checkDeposits();
    await processWithdrawals();
    await checkAndSweepHotToWarm();
    await checkAndSweepWarmToCold();
  } catch(e) {
    console.error('[Sync] Cycle error:', e.message);
  }
}

// ─── INTER-SERVICE CALL ───────────────────────────────────────────────────────
function callLedger(path, body) {
  return new Promise((resolve, reject) => {
    const data = JSON.stringify(body);
    const req  = http.request({
      hostname: '127.0.0.1', port: LEDGER_PORT, path, method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Content-Length': Buffer.byteLength(data) },
    }, res => { let d=''; res.on('data',c=>d+=c); res.on('end',()=>resolve(JSON.parse(d))); });
    req.on('error', reject);
    req.write(data); req.end();
  });
}

// ─── HTTP API ─────────────────────────────────────────────────────────────────
function readBody(req) {
  return new Promise((res,rej)=>{ let d=''; req.on('data',c=>d+=c); req.on('end',()=>{ try{res(JSON.parse(d||'{}'));}catch(e){rej(e);} }); });
}
function send(res, status, body) {
  res.writeHead(status,{'Content-Type':'application/json'}); res.end(JSON.stringify(body));
}

const server = http.createServer(async (req, res) => {
  const url = req.url, method = req.method;

  // POST /withdraw  { accountId, toAddress, amount, currency }
  if (method === 'POST' && url === '/withdraw') {
    const { accountId, toAddress, amount, currency = 'ETH' } = await readBody(req);
    if (!accountId || !toAddress || !amount) return send(res, 400, { error: 'missing fields' });

    // Screen immediately on request
    const screen = screenAddress(toAddress, accountId, amount);
    if (!screen.clean && screen.issues.includes('ADDRESS_BLACKLISTED')) {
      return send(res, 403, { error: 'ADDRESS_BLOCKED', issues: screen.issues });
    }

    const w = {
      id: crypto.randomUUID(), accountId, toAddress,
      amount, currency, status: 'PENDING', createdAt: Date.now(),
    };
    withdrawalQueue.push(w);
    return send(res, 202, { ok: true, withdrawalId: w.id, status: 'PENDING',
      message: 'Queued for next settlement cycle' });
  }

  // POST /deposit  { accountId, txHash, fromAddress, amount, currency }
  if (method === 'POST' && url === '/deposit') {
    const { accountId, txHash, fromAddress, amount, currency = 'ETH' } = await readBody(req);
    const screen = screenAddress(fromAddress, accountId, amount);
    if (!screen.clean && screen.issues.includes('ADDRESS_BLACKLISTED')) {
      return send(res, 403, { error: 'ADDRESS_BLOCKED', issues: screen.issues });
    }
    const d = { id: crypto.randomUUID(), accountId, txHash, fromAddress, amount, currency,
                confirmations: 0, status: 'PENDING', creditedAt: null, createdAt: Date.now() };
    depositQueue.push(d);
    return send(res, 202, { ok: true, depositId: d.id, status: 'PENDING' });
  }

  // GET /wallet-balances
  if (method === 'GET' && url === '/wallet-balances') {
    return send(res, 200, {
      hot:  { balance: walletBalances.hot,  address: WALLET_CONFIG.hot.address,  sweepThreshold: WALLET_CONFIG.hot.sweepThreshold  },
      warm: { balance: walletBalances.warm, address: WALLET_CONFIG.warm.address, sweepThreshold: WALLET_CONFIG.warm.sweepThreshold },
      cold: { balance: walletBalances.cold, address: WALLET_CONFIG.cold.address, note: 'manual-only outbound' },
      total: walletBalances.hot + walletBalances.warm + walletBalances.cold,
    });
  }

  // GET /settlement-log
  if (method === 'GET' && url === '/settlement-log') {
    return send(res, 200, { entries: settlementLog.slice(-100), total: settlementLog.length });
  }

  // GET /flagged-accounts  (admin only — add auth in production)
  if (method === 'GET' && url === '/flagged-accounts') {
    return send(res, 200, { flagged: Object.fromEntries(flaggedAccounts) });
  }

  // GET /withdrawal/:id
  if (method === 'GET' && url.startsWith('/withdrawal/')) {
    const id = url.split('/')[2];
    const w  = withdrawalQueue.find(w => w.id === id);
    return send(res, w ? 200 : 404, w || { error: 'not found' });
  }

  // POST /run-cycle  (manual trigger for testing)
  if (method === 'POST' && url === '/run-cycle') {
    await runSettlementCycle();
    return send(res, 200, { ok: true, settlementLog: settlementLog.slice(-10) });
  }

  // GET /health
  if (url === '/health') return send(res, 200, { status: 'ok', service: 'wallet-sync', walletBalances });

  send(res, 404, { error: 'not found' });
});

server.listen(PORT, () => {
  console.log(`[Sync]    http://localhost:${PORT}`);
  console.log(`[Sync]    Settlement interval: ${SETTLEMENT_INTERVAL_MS / 1000}s`);
});

// Start settlement loop
setInterval(runSettlementCycle, SETTLEMENT_INTERVAL_MS);
// First cycle after 5s warmup
setTimeout(runSettlementCycle, 5000);
