'use strict';
/**
 * GATEWAY SERVICE
 * Authenticates players, manages WebSocket connections, rate-limits,
 * and proxies spin requests to the Math Service.
 *
 * In production: sit behind Cloudflare. Replace token auth with JWT/OAuth.
 * Replace inter-service HTTP with NATS/Kafka publish.
 */

const http   = require('http');
const { WebSocketServer } = require('ws');
const crypto = require('crypto');
const path   = require('path');
const fs     = require('fs');

const PORT      = process.env.PORT       || 3000;
const MATH_PORT = process.env.MATH_PORT  || 3001;
const LEDGER_PORT = process.env.LEDGER_PORT || 3002;

// ─── RATE LIMITER ─────────────────────────────────────────────────────────────
// Token bucket per accountId: burst of 3, refills 1 token/sec.
// Duplicate spin within 200ms: dropped before touching any service.
const rateBuckets   = new Map();  // accountId → { tokens, lastRefill }
const spinTimestamp = new Map();  // accountId → lastSpinMs (duplicate guard)

function checkRateLimit(accountId) {
  const now    = Date.now();
  const bucket = rateBuckets.get(accountId) || { tokens: 3, lastRefill: now };

  // Refill tokens based on elapsed time
  const elapsed = (now - bucket.lastRefill) / 1000;
  bucket.tokens = Math.min(3, bucket.tokens + elapsed);
  bucket.lastRefill = now;
  rateBuckets.set(accountId, bucket);

  if (bucket.tokens < 1) return false;
  bucket.tokens -= 1;
  return true;
}

function checkDuplicate(accountId) {
  const last = spinTimestamp.get(accountId) || 0;
  if (Date.now() - last < 200) return true; // duplicate
  spinTimestamp.set(accountId, Date.now());
  return false;
}

// ─── AUTH (stub — replace with JWT verification in production) ────────────────
const sessions = new Map(); // token → accountId

function createSession(accountId) {
  const token = crypto.randomBytes(16).toString('hex');
  sessions.set(token, accountId);
  return token;
}

function authenticate(req) {
  const token = req.headers['x-auth-token'] || req.headers['x-user-id'];
  if (!token) return null;
  // In production: verify JWT, check expiry, validate signature
  if (!sessions.has(token)) {
    // Auto-create session for demo (remove in production)
    sessions.set(token, token.slice(0, 16));
  }
  return sessions.get(token);
}

// ─── INTER-SERVICE CALLS ──────────────────────────────────────────────────────
function callService(port, path, method, body) {
  return new Promise((resolve, reject) => {
    const data = body ? JSON.stringify(body) : null;
    const opts = {
      hostname: '127.0.0.1', port, path, method,
      headers: {
        'Content-Type': 'application/json',
        ...(data ? { 'Content-Length': Buffer.byteLength(data) } : {}),
      },
    };
    const req = http.request(opts, res => {
      let d = '';
      res.on('data', c => d += c);
      res.on('end', () => {
        try { resolve({ status: res.statusCode, body: JSON.parse(d) }); }
        catch(e) { resolve({ status: res.statusCode, body: {} }); }
      });
    });
    req.on('error', reject);
    if (data) req.write(data);
    req.end();
  });
}

// ─── WEBSOCKET LIVE FEED ──────────────────────────────────────────────────────
const wsClients = new Map(); // accountId → Set<ws>
const app    = http.createServer(handleHTTP);
const wss    = new WebSocketServer({ server: app });

wss.on('connection', (ws, req) => {
  const token     = new URL(req.url, 'http://x').searchParams.get('token') || req.headers['x-auth-token'];
  const accountId = sessions.get(token) || token?.slice(0,16) || 'anon';

  if (!wsClients.has(accountId)) wsClients.set(accountId, new Set());
  wsClients.get(accountId).add(ws);

  ws.on('close', () => wsClients.get(accountId)?.delete(ws));
  ws.on('error', () => wsClients.get(accountId)?.delete(ws));
  ws.isAlive = true;
  ws.on('pong', () => { ws.isAlive = true; });
});

// Heartbeat prune
setInterval(() => {
  for (const [id, clients] of wsClients) {
    for (const ws of clients) {
      if (!ws.isAlive) { ws.terminate(); clients.delete(ws); }
      else { ws.isAlive = false; ws.ping(); }
    }
  }
}, 30000);

function pushToClient(accountId, payload) {
  const clients = wsClients.get(accountId);
  if (!clients) return;
  const msg = JSON.stringify(payload);
  for (const ws of clients) if (ws.readyState === 1) ws.send(msg);
}

function broadcastAll(payload) {
  const msg = JSON.stringify(payload);
  for (const clients of wsClients.values())
    for (const ws of clients) if (ws.readyState === 1) ws.send(msg);
}

// ─── STATIC FILES ─────────────────────────────────────────────────────────────
const STATIC_ROOT = path.join(__dirname, '..');
const MIME = { '.html':'text/html', '.js':'application/javascript', '.css':'text/css', '.json':'application/json' };

function serveStatic(req, res) {
  const filePath = path.join(STATIC_ROOT, req.url === '/' ? '/neon-overload-connected.html' : req.url);
  fs.readFile(filePath, (err, data) => {
    if (err) { res.writeHead(404); res.end('Not found'); return; }
    const ext = path.extname(filePath);
    res.writeHead(200, { 'Content-Type': MIME[ext] || 'text/plain' });
    res.end(data);
  });
}

// ─── HTTP HANDLER ─────────────────────────────────────────────────────────────
function readBody(req) {
  return new Promise((res, rej) => {
    let d = '';
    req.on('data', c => d += c);
    req.on('end', () => { try { res(JSON.parse(d||'{}')); } catch(e) { rej(e); } });
  });
}
function send(res, status, body) {
  res.writeHead(status, { 'Content-Type':'application/json', 'Access-Control-Allow-Origin':'*' });
  res.end(JSON.stringify(body));
}

async function handleHTTP(req, res) {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type, x-auth-token, x-user-id');
  if (req.method === 'OPTIONS') { res.writeHead(204); res.end(); return; }

  const url    = req.url.split('?')[0];
  const method = req.method;

  // ── AUTH ENDPOINT ──
  if (method === 'POST' && url === '/auth') {
    const { userId } = await readBody(req);
    const id    = userId || crypto.randomBytes(8).toString('hex');
    const token = createSession(id);
    return send(res, 200, { token, accountId: id });
  }

  // ── SESSION / SEEDS ──
  if (method === 'GET' && url === '/session') {
    const accountId = authenticate(req);
    if (!accountId) return send(res, 401, { error: 'unauthorized' });
    const [acct, seeds] = await Promise.all([
      callService(LEDGER_PORT, `/account/${accountId}`, 'GET'),
      callService(MATH_PORT,   `/seeds/${accountId}`,   'GET'),
    ]);
    return send(res, 200, {
      accountId,
      balance:        acct.body.balance,
      serverSeedHash: seeds.body.serverSeedHash,
      clientSeed:     seeds.body.clientSeed,
      nonce:          seeds.body.nonce,
    });
  }

  // ── SPIN ── (the hot path)
  if (method === 'POST' && url === '/spin') {
    const accountId = authenticate(req);
    if (!accountId) return send(res, 401, { error: 'unauthorized' });

    // Drop duplicate clicks before any service call
    if (checkDuplicate(accountId)) {
      return send(res, 429, { error: 'duplicate_spin', code: 'DUPLICATE' });
    }

    // Token bucket rate limit
    if (!checkRateLimit(accountId)) {
      return send(res, 429, { error: 'rate_limited', code: 'RATE_LIMIT' });
    }

    let body;
    try { body = await readBody(req); } catch(e) { return send(res, 400, { error: 'bad json' }); }

    // Forward to Math Service (which coordinates with Ledger)
    let result;
    try {
      const r = await callService(MATH_PORT, '/spin', 'POST', { ...body, accountId });
      result  = r.body;
      if (!result.ok) return send(res, r.status || 400, result);
    } catch(e) {
      return send(res, 503, { error: 'math_service_unavailable' });
    }

    // Broadcast win to live feed
    if (result.manifest?.total_win > 0) {
      broadcastAll({
        type:      'WIN',
        userId:    accountId.slice(0, 8),
        mult:      Math.round((result.manifest.total_win / body.bet) * 10) / 10,
        amount:    result.manifest.total_win,
        ts:        Date.now(),
      });
      // Also push full manifest privately to the player
      pushToClient(accountId, { type: 'SPIN_RESULT', ...result });
    }

    return send(res, 200, result);
  }

  // ── CLIENT SEED ROTATION ──
  if (method === 'PATCH' && url === '/client-seed') {
    const accountId = authenticate(req);
    if (!accountId) return send(res, 401, { error: 'unauthorized' });
    const body = await readBody(req);
    const r = await callService(MATH_PORT, '/client-seed', 'PATCH', { ...body, accountId });
    return send(res, 200, r.body);
  }

  // ── VERIFY ──
  if (method === 'GET' && url.startsWith('/verify/')) {
    const serverSeed = url.split('/')[2];
    const qs = new URL(req.url, 'http://x').searchParams;
    const clientSeed = qs.get('clientSeed'), nonce = qs.get('nonce');
    if (!clientSeed || !nonce) return send(res, 400, { error: 'clientSeed and nonce required' });
    const { hmacSHA256, sha256 } = require('../engine/math');
    const hmac = hmacSHA256(serverSeed, `${clientSeed}:${nonce}:0`);
    return send(res, 200, { serverSeed, clientSeed, nonce: Number(nonce), hmacHex: hmac, sha256Hash: sha256(serverSeed) });
  }

  // ── LEDGER PASSTHROUGH (read-only) ──
  if (method === 'GET' && url.startsWith('/ledger/')) {
    const accountId = authenticate(req);
    if (!accountId) return send(res, 401, { error: 'unauthorized' });
    const r = await callService(LEDGER_PORT, url, 'GET');
    return send(res, r.status, r.body);
  }

  // ── RTP VALIDATION (admin) ──
  if (method === 'POST' && url === '/validate-rtp') {
    const body = await readBody(req);
    const r = await callService(MATH_PORT, '/validate-rtp', 'POST', body);
    return send(res, 200, r.body);
  }

  // ── HEALTH ──
  if (url === '/health') return send(res, 200, { status: 'ok', service: 'gateway' });

  // ── STATIC FILES ──
  serveStatic(req, res);
}

app.listen(PORT, () => {
  console.log(`[Gateway] http://localhost:${PORT}`);
  console.log(`          → Math Service   :${MATH_PORT}`);
  console.log(`          → Ledger Service :${LEDGER_PORT}`);
});
