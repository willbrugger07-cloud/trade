'use strict';
/**
 * MATH & ENGINE SERVICE
 * Pure calculation core. Receives spin parameters, returns Playback Manifest.
 * Zero balance logic here — delegates all money ops to Ledger Service.
 *
 * Inter-service: HTTP on MATH_PORT (default 3001).
 * In production replace HTTP calls with NATS/Kafka publish/subscribe.
 */

const http   = require('http');
const crypto = require('crypto');

const {
  buildWeights, generateManifest, runMathSimulation, validateRTP,
  HOUSE_EDGE, SYM,
} = require('../engine/math');

const PORT         = process.env.MATH_PORT   || 3001;
const LEDGER_PORT  = process.env.LEDGER_PORT || 3002;

// ─── INTER-SERVICE HTTP CALL ──────────────────────────────────────────────────
// In production: replace with NATS.publish() / Kafka.produce()
function callLedger(path, body) {
  return new Promise((resolve, reject) => {
    const data = JSON.stringify(body);
    const req  = http.request({
      hostname: '127.0.0.1', port: LEDGER_PORT,
      path, method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Content-Length': Buffer.byteLength(data) },
    }, res => {
      let d = '';
      res.on('data', c => d += c);
      res.on('end', () => resolve({ status: res.statusCode, body: JSON.parse(d) }));
    });
    req.on('error', reject);
    req.write(data); req.end();
  });
}

// ─── SEED STORE (per-account) ────────────────────────────────────────────────
// In production: Redis hash keyed by accountId
const seeds = new Map();
function getSeeds(accountId) {
  if (!seeds.has(accountId)) {
    const serverSeed = crypto.randomBytes(32).toString('hex');
    seeds.set(accountId, {
      serverSeed,
      serverSeedHash: crypto.createHash('sha256').update(serverSeed).digest('hex'),
      clientSeed:     crypto.randomBytes(8).toString('hex'),
      nonce:          0,
    });
  }
  return seeds.get(accountId);
}

// ─── DYNAMIC VOLATILITY SCALING ──────────────────────────────────────────────
/**
 * Scales capacitor/multiplier weight down as consecutive cascade wins climb.
 * Prevents runaway variance while protecting house edge.
 * Based on the pattern from the specification.
 */
function applyDynamicScaling(weights, consecutiveCascades) {
  const w = [...weights];
  const nerfFactor = Math.max(0.1, 1.0 - consecutiveCascades * 0.15);
  // Cap: weight[CAPACITOR] * nerfFactor, floor at 10
  w[SYM.CAPACITOR] = Math.max(10, Math.floor(w[SYM.CAPACITOR] * nerfFactor));
  return w;
}

// ─── GRID SIZE ────────────────────────────────────────────────────────────────
function gridSize(volatility, inPrism) {
  if (inPrism)         return { rows: 8, cols: 8 };
  if (volatility >= 8) return { rows: 4, cols: 4 };
  return { rows: 6, cols: 6 };
}

// ─── SPIN HANDLER ─────────────────────────────────────────────────────────────
async function handleSpin(params) {
  const {
    accountId, bet, volatility = 5,
    anteBet = false, multiChaser = false,
    inPrism = false, prismStartMult = 1,
    consecutiveCascades = 0,     // passed from client session state
  } = params;

  const effectiveBet = anteBet ? bet * 1.25 : bet;

  // 1. DEBIT via Ledger Service
  const debitRes = await callLedger('/debit', { accountId, wager: effectiveBet });
  if (!debitRes.body.ok) return { ok: false, ...debitRes.body };
  const { roundId, balanceAfter: balanceAfterDebit } = debitRes.body;

  let manifest;
  try {
    // 2. GENERATE MANIFEST (pure math, no I/O)
    const s       = getSeeds(accountId);
    const { rows, cols } = gridSize(volatility, inPrism);
    let   weights = buildWeights(volatility, { anteBet, multiChaser, bonusHighPay: inPrism });
    weights       = applyDynamicScaling(weights, consecutiveCascades);

    // Snapshot seeds before mutation
    const { serverSeed, clientSeed, nonce } = s;

    manifest = generateManifest({
      serverSeed, clientSeed, nonce,
      bet: effectiveBet, weights, rows, cols,
      startVoltMult: prismStartMult,
      multiChaser,
      ticketId: roundId,   // round ID doubles as ticket ID for auditability
    });

    // Rotate server seed after reveal (provably fair commitment cycle)
    const newServerSeed    = crypto.randomBytes(32).toString('hex');
    s.nonce++;
    s.serverSeed           = newServerSeed;
    s.serverSeedHash       = crypto.createHash('sha256').update(newServerSeed).digest('hex');

    manifest.next_server_seed_hash = s.serverSeedHash;
    manifest.client_seed           = clientSeed;

  } catch (err) {
    // Math engine error: rollback the debit
    await callLedger('/rollback', { roundId, reason: err.message });
    return { ok: false, error: 'ENGINE_ERROR', detail: err.message, code: 500 };
  }

  // 3. CREDIT via Ledger Service
  const creditRes = await callLedger('/credit', {
    roundId,
    payout: manifest.total_win,
    manifest: {
      ticket_id:      manifest.ticket_id,
      cascades:       manifest.cascades.length,
      final_volt_mult: manifest.final_volt_mult,
      bonus_triggered: manifest.bonus_triggered,
    },
  });

  if (!creditRes.body.ok) {
    // Critical: credit failed after math ran — alert + manual review queue in production
    console.error('[CRITICAL] Credit failed for round', roundId, creditRes.body);
    return { ok: false, error: 'CREDIT_FAILED', roundId, code: 500 };
  }

  return {
    ok:           true,
    manifest,                                    // full Playback Manifest → client
    balance_after: creditRes.body.balanceAfter,
    house_edge:    HOUSE_EDGE,
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

const server = http.createServer(async (req, res) => {
  const url = req.url, method = req.method;

  // POST /spin
  if (method === 'POST' && url === '/spin') {
    try {
      const params = await readBody(req);
      const result = await handleSpin(params);
      return send(res, result.ok ? 200 : (result.code || 400), result);
    } catch (e) {
      return send(res, 500, { ok: false, error: e.message });
    }
  }

  // GET /seeds/:accountId  — current seed state for verify UI
  if (method === 'GET' && url.startsWith('/seeds/')) {
    const accountId = url.split('/')[2];
    const s = getSeeds(accountId);
    return send(res, 200, {
      serverSeedHash: s.serverSeedHash,
      clientSeed:     s.clientSeed,
      nonce:          s.nonce,
    });
  }

  // PATCH /client-seed  { accountId, clientSeed }
  if (method === 'PATCH' && url === '/client-seed') {
    const { accountId, clientSeed } = await readBody(req);
    const s = getSeeds(accountId);
    const newServer  = crypto.randomBytes(32).toString('hex');
    s.clientSeed     = (clientSeed || crypto.randomBytes(8).toString('hex')).slice(0, 64);
    s.serverSeed     = newServer;
    s.serverSeedHash = crypto.createHash('sha256').update(newServer).digest('hex');
    s.nonce          = 0;
    return send(res, 200, { clientSeed: s.clientSeed, serverSeedHash: s.serverSeedHash, nonce: 0 });
  }

  // POST /validate-rtp  { spins, volatility, tolerance }
  // Runs in-memory simulation — bypasses all network/DB
  if (method === 'POST' && url === '/validate-rtp') {
    const { spins = 100_000, volatility = 5, tolerance = 0.05 } = await readBody(req);
    const report = validateRTP(spins, { volatility, tolerance, verbose: false });
    return send(res, 200, report);
  }

  // GET /health
  if (url === '/health') return send(res, 200, { status: 'ok', service: 'math-engine' });

  send(res, 404, { error: 'not found' });
});

server.listen(PORT, () => console.log(`[Math]    http://localhost:${PORT}`));
