'use strict';
/**
 * MODULE 2 & 3: STATE MACHINE, ANTI-EXPLOIT CONTROLLER, HTTP API, WEBSOCKET LAYER
 *
 * Run:  node engine/server.js
 * Deps: npm install express ws uuid
 *
 * In production replace in-memory stores with:
 *   - userStore    → PostgreSQL/MySQL with row-level locking (SELECT FOR UPDATE)
 *   - sessionStore → Redis with atomic SETNX for spin locks
 *   - pubSub       → Redis Pub/Sub for cross-instance live feed
 */

const express   = require('express');
const http      = require('http');
const { WebSocketServer } = require('ws');
const crypto    = require('crypto');
const path      = require('path');

const {
  buildWeights, generateManifest, sha256, HOUSE_EDGE,
} = require('./math');

// Grid size by volatility
function gridSize(volatility, inPrism) {
  if (inPrism)         return { rows: 8, cols: 8 };
  if (volatility >= 8) return { rows: 4, cols: 4 };
  return { rows: 6, cols: 6 };
}

const app    = express();
const server = http.createServer(app);
const wss    = new WebSocketServer({ server });

app.use(express.json());
app.use(express.static(path.join(__dirname, '..')));

// ─── IN-MEMORY STORES (replace with DB in production) ────────────────────────
const users = new Map();       // userId → { balance, status, serverSeed, nonce, clientSeed, seedHash }
const spinLocks = new Map();   // userId → true  (second lock layer; primary is user.status)
const liveFeedClients = new Set(); // WebSocket clients subscribed to live feed

function getUser(userId) {
  if (!users.has(userId)) {
    const serverSeed = crypto.randomBytes(32).toString('hex');
    users.set(userId, {
      id: userId,
      balance: 1000.00,
      status: 'IDLE',           // IDLE | LOCKED_SPINNING
      serverSeed,
      serverSeedHash: sha256(serverSeed),
      clientSeed: crypto.randomBytes(8).toString('hex'),
      nonce: 0,
    });
  }
  return users.get(userId);
}

// ─── ATOMIC SPIN LOCK ─────────────────────────────────────────────────────────
// In production: use Redis SETNX with TTL so stale locks self-expire.
function acquireLock(userId) {
  if (spinLocks.get(userId)) return false;
  spinLocks.set(userId, true);
  return true;
}
function releaseLock(userId) { spinLocks.delete(userId); }

// ─── LIVE FEED BROADCAST ─────────────────────────────────────────────────────
function broadcast(payload) {
  // In production: Redis PUBLISH "live_feed" JSON.stringify(payload)
  const msg = JSON.stringify(payload);
  for (const ws of liveFeedClients) {
    if (ws.readyState === 1) ws.send(msg);
  }
}

// ─── WEBSOCKET HANDLER ───────────────────────────────────────────────────────
wss.on('connection', (ws, req) => {
  liveFeedClients.add(ws);
  ws.on('close', () => liveFeedClients.delete(ws));
  ws.on('error', () => liveFeedClients.delete(ws));

  // Heartbeat ping
  ws.isAlive = true;
  ws.on('pong', () => { ws.isAlive = true; });
});

// Prune dead connections every 30s
setInterval(() => {
  for (const ws of liveFeedClients) {
    if (!ws.isAlive) { ws.terminate(); liveFeedClients.delete(ws); continue; }
    ws.isAlive = false;
    ws.ping();
  }
}, 30000);

// ─── ROUTES ──────────────────────────────────────────────────────────────────

// GET /session  — initialise or return session info (no sensitive seeds exposed)
app.get('/session', (req, res) => {
  const userId = req.headers['x-user-id'] || 'demo';
  const user   = getUser(userId);
  res.json({
    userId:         user.id,
    balance:        user.balance,
    serverSeedHash: user.serverSeedHash,   // commitment hash shown BEFORE spin
    clientSeed:     user.clientSeed,
    nonce:          user.nonce,
  });
});

// PATCH /client-seed  — player rotates their client seed
app.patch('/client-seed', (req, res) => {
  const userId = req.headers['x-user-id'] || 'demo';
  const user   = getUser(userId);
  if (user.status === 'LOCKED_SPINNING') return res.status(409).json({ error: 'spin in progress' });

  const newSeed    = (req.body.clientSeed || crypto.randomBytes(8).toString('hex')).slice(0, 64);
  const newServer  = crypto.randomBytes(32).toString('hex');
  user.clientSeed      = newSeed;
  user.serverSeed      = newServer;
  user.serverSeedHash  = sha256(newServer);
  user.nonce           = 0;
  res.json({ clientSeed: user.clientSeed, serverSeedHash: user.serverSeedHash, nonce: 0 });
});

/**
 * POST /spin
 *
 * Body: {
 *   bet:          number,
 *   volatility:   1-10,
 *   anteBet:      bool,
 *   multiChaser:  bool,
 *   inPrism:      bool,
 *   prismStartMult: number,
 * }
 *
 * Response: full cascade history — client plays it back deterministically.
 */
app.post('/spin', (req, res) => {
  const userId = req.headers['x-user-id'] || 'demo';
  const user   = getUser(userId);

  // ── LAYER 1: state check (avoids even hitting the lock) ──
  if (user.status === 'LOCKED_SPINNING') {
    return res.status(429).json({ error: 'spin already in progress', code: 'IN_PROGRESS' });
  }

  // ── LAYER 2: atomic lock (guards concurrent requests) ────
  if (!acquireLock(userId)) {
    return res.status(429).json({ error: 'spin already in progress', code: 'LOCKED' });
  }

  const { bet = 1.00, volatility = 5, anteBet = false, multiChaser = false,
          inPrism = false, prismStartMult = 1 } = req.body;

  const effectiveBet = anteBet ? bet * 1.25 : bet;

  // ── BALANCE CHECK ─────────────────────────────────────────
  if (user.balance < effectiveBet) {
    releaseLock(userId);
    return res.status(402).json({ error: 'insufficient balance' });
  }

  // ── ATOMIC DEBIT + STATUS LOCK ────────────────────────────
  // In production: wrap in DB transaction (BEGIN / SELECT FOR UPDATE / UPDATE / COMMIT)
  user.balance = Math.round((user.balance - effectiveBet) * 100) / 100;
  user.status  = 'LOCKED_SPINNING';

  // Snapshot seeds before any mutation
  const serverSeed = user.serverSeed;
  const clientSeed = user.clientSeed;
  const nonce      = user.nonce;

  // ── GENERATE MANIFEST (all math runs here — client gets a movie) ─
  const { rows, cols } = gridSize(volatility, inPrism);
  const weights = buildWeights(volatility, { anteBet, multiChaser, bonusHighPay: inPrism });
  const result  = generateManifest({
    serverSeed, clientSeed, nonce,
    bet: effectiveBet, weights, rows, cols,
    startVoltMult: prismStartMult,
    multiChaser,
    ticketId: crypto.randomUUID(),
  });

  // ── ADVANCE NONCE + ROTATE SERVER SEED ───────────────────
  user.nonce++;
  const newServerSeed     = crypto.randomBytes(32).toString('hex');
  const prevServerSeed    = serverSeed;
  user.serverSeed         = newServerSeed;
  user.serverSeedHash     = sha256(newServerSeed);

  // ── CREDIT WIN ────────────────────────────────────────────
  user.balance = Math.round((user.balance + result.total_win) * 100) / 100;

  // ── RELEASE LOCK ─────────────────────────────────────────
  user.status = 'IDLE';
  releaseLock(userId);

  // ── BROADCAST TO LIVE FEED ───────────────────────────────
  if (result.total_win > 0) {
    broadcast({
      type:   'WIN',
      userId: userId.slice(0, 8),
      mult:   Math.round((result.total_win / effectiveBet) * 10) / 10,
      amount: result.total_win,
      ts:     Date.now(),
    });
  }

  // ── RESPONSE: Playback Manifest (client is a dumb movie player) ──
  res.json({
    ...result,                            // full manifest: ticket_id, initial_grid, cascades[], etc.
    server_seed_reveal: prevServerSeed,   // safe to reveal now
    next_server_seed_hash: user.serverSeedHash,
    balance_after: user.balance,
    house_edge: HOUSE_EDGE,
  });
});

// GET /verify/:serverSeed  — client-side verification helper
app.get('/verify/:serverSeed', (req, res) => {
  const { serverSeed } = req.params;
  const { clientSeed, nonce } = req.query;
  if (!clientSeed || nonce === undefined) return res.status(400).json({ error: 'clientSeed and nonce required' });
  const { hmacSHA256 } = require('./math');

  const hex = hmacSHA256(serverSeed, `${clientSeed}:${nonce}`);
  res.json({ serverSeed, clientSeed, nonce: Number(nonce), hmacHex: hex, sha256Hash: sha256(serverSeed) });
});

// ─── START ───────────────────────────────────────────────────────────────────
const PORT = process.env.PORT || 3000;
server.listen(PORT, () => console.log(`Neon Overload running on http://localhost:${PORT}`));

module.exports = { app, server };
