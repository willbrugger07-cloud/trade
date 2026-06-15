'use strict';
/**
 * NEON OVERLOAD — HIGH-SPEED MATH ENGINE v2
 * Flat integer array grid, bitwise-friendly operations, Playback Manifest generator,
 * and standalone RTP simulation tool.
 *
 * Run simulation: node math.js --simulate 1000000
 */

const crypto = require('crypto');

// ─── SYMBOL IDS ───────────────────────────────────────────────────────────────
const SYM = {
  EMPTY:     0,
  TRIANGLE:  1,  // low,  pay 0.20
  SQUARE:    2,  // low,  pay 0.30
  CIRCLE:    3,  // mid,  pay 0.50
  DIAMOND:   4,  // mid,  pay 0.80
  HEXAGON:   5,  // high, pay 2.00
  CAPACITOR: 6,  // special — voltage multiplier
  WILD:      7,  // special — matches any regular symbol
  BLACKHOLE: 8,  // special — removes low-pays after cascades
  SCATTER:   9,  // special — 3+ triggers Prism Phase
};
const SYM_NAME = Object.fromEntries(Object.entries(SYM).map(([k,v])=>[v,k.toLowerCase()]));

const PAY = {
  [SYM.TRIANGLE]:  0.20,
  [SYM.SQUARE]:    0.30,
  [SYM.CIRCLE]:    0.50,
  [SYM.DIAMOND]:   0.80,
  [SYM.HEXAGON]:   2.00,
};
const CAPACITOR_VALUES = [2, 5, 10];
const LOW_TIER  = new Set([SYM.TRIANGLE, SYM.SQUARE]);
const SPECIAL   = new Set([SYM.CAPACITOR, SYM.WILD, SYM.BLACKHOLE, SYM.SCATTER]);

const MIN_CLUSTER    = 5;
const MAX_WIN_MULT   = 10000;

// ─── HOUSE EDGE CONFIG ────────────────────────────────────────────────────────
// Adjust this to control RTP. 0.035 → ~96.5% RTP target.
const HOUSE_EDGE = 0.035;

// ─── WEIGHT PRESETS (indexed by SYM id) ──────────────────────────────────────
const WEIGHT_PRESETS = {
  low: [
    0,    // EMPTY — never picked directly
    3500, // TRIANGLE
    3500, // SQUARE
    1400, // CIRCLE
    1000, // DIAMOND
    300,  // HEXAGON
    200,  // CAPACITOR
    100,  // WILD
    0,    // BLACKHOLE
    0,    // SCATTER
  ],
  medium: [
    0,
    2800, 2700, 1500, 1300, 800, 400, 300, 100, 100,
  ],
  high: [
    0,
    800, 800, 1600, 1800, 2800, 1000, 800, 200, 200,
  ],
};

function applyHouseEdge(weights, edge) {
  const w = [...weights];
  const shift = Math.floor(edge * 10000);
  w[SYM.HEXAGON]  = Math.max(50, w[SYM.HEXAGON]  - shift);
  w[SYM.DIAMOND]  = Math.max(50, w[SYM.DIAMOND]  - Math.floor(shift * 0.5));
  w[SYM.TRIANGLE] += shift;
  w[SYM.SQUARE]   += Math.floor(shift * 0.5);
  return w;
}

function buildWeights(volatility, opts = {}) {
  let preset;
  if (volatility <= 3)      preset = 'low';
  else if (volatility <= 7) preset = 'medium';
  else                      preset = 'high';

  let w = applyHouseEdge([...WEIGHT_PRESETS[preset]], HOUSE_EDGE);
  if (opts.anteBet)      w[SYM.CAPACITOR] = Math.min(w[SYM.CAPACITOR] * 2, 1500);
  if (opts.multiChaser)  w[SYM.CAPACITOR] = 3000;
  if (opts.bonusHighPay) {
    w[SYM.CIRCLE]   = Math.floor(w[SYM.CIRCLE]   * 1.3);
    w[SYM.DIAMOND]  = Math.floor(w[SYM.DIAMOND]  * 1.3);
    w[SYM.HEXAGON]  = Math.floor(w[SYM.HEXAGON]  * 1.5);
    w[SYM.TRIANGLE] = Math.max(10, Math.floor(w[SYM.TRIANGLE] * 0.4));
    w[SYM.SQUARE]   = Math.max(10, Math.floor(w[SYM.SQUARE]   * 0.4));
  }
  return w;
}

// ─── RNG ──────────────────────────────────────────────────────────────────────
function hmacSHA256(serverSeed, message) {
  return crypto.createHmac('sha256', serverSeed).update(message).digest('hex');
}
function sha256(str) {
  return crypto.createHash('sha256').update(str).digest('hex');
}

function buildCumulative(weights) {
  const cum = new Int32Array(weights.length);
  let acc = 0;
  for (let i = 0; i < weights.length; i++) { acc += weights[i]; cum[i] = acc; }
  return cum;
}
function pickFromCum(cum, r) {
  for (let i = 1; i < cum.length; i++) if (r < cum[i]) return i;
  return 1;
}

/**
 * Generate flat Int32Array grid using chained HMAC calls.
 * Each 4-byte chunk of the HMAC hex → one symbol pick.
 * Chains additional HMAC calls if grid > 8 cells.
 */
function generateGrid(serverSeed, clientSeed, nonce, weights, rows, cols) {
  const size  = rows * cols;
  const grid  = new Int32Array(size);
  const cum   = buildCumulative(weights);
  const total = weights.reduce((a, b) => a + b, 0);
  let filled  = 0, chain = 0;

  while (filled < size) {
    const hex   = hmacSHA256(serverSeed, `${clientSeed}:${nonce}:${chain++}`);
    const bytes = Buffer.from(hex, 'hex');
    for (let i = 0; i + 4 <= bytes.length && filled < size; i += 4) {
      const val = (bytes[i]<<24 | bytes[i+1]<<16 | bytes[i+2]<<8 | bytes[i+3]) >>> 0;
      grid[filled++] = pickFromCum(cum, val % total);
    }
  }
  return grid;
}

// Flat index helper
const flatIdx = (r, c, cols) => r * cols + c;

// ─── CLUSTER DETECTION (flood fill on flat Int32Array) ───────────────────────
function findClusters(grid, rows, cols) {
  const visited  = new Uint8Array(rows * cols);
  const clusters = [];

  for (let i = 0; i < grid.length; i++) {
    const sym = grid[i];
    if (!sym || visited[i] || SPECIAL.has(sym)) continue;

    const cells = [];
    const stack = [i];
    visited[i]  = 1;

    while (stack.length) {
      const pos = stack.pop();
      cells.push(pos);
      const r = (pos / cols) | 0, c = pos % cols;

      // 4-directional neighbors
      if (r > 0)      checkNeighbor(pos - cols, sym, grid, visited, stack);
      if (r < rows-1) checkNeighbor(pos + cols, sym, grid, visited, stack);
      if (c > 0)      checkNeighbor(pos - 1,    sym, grid, visited, stack);
      if (c < cols-1) checkNeighbor(pos + 1,    sym, grid, visited, stack);
    }
    if (cells.length >= MIN_CLUSTER) clusters.push({ symbolId: sym, cells });
  }
  return clusters;
}

function checkNeighbor(n, sym, grid, visited, stack) {
  if (visited[n]) return;
  const ns = grid[n];
  if (!ns || SPECIAL.has(ns)) return;
  if (ns === sym || ns === SYM.WILD) { visited[n] = 1; stack.push(n); }
}

// ─── PULSE GRAVITY (in-place, column-wise) ───────────────────────────────────
function applyGravity(grid, rows, cols) {
  for (let c = 0; c < cols; c++) {
    const col = [];
    for (let r = rows - 1; r >= 0; r--) { const v = grid[flatIdx(r,c,cols)]; if (v) col.push(v); }
    for (let r = rows - 1; r >= 0; r--) grid[flatIdx(r,c,cols)] = col.length ? col.shift() : SYM.EMPTY;
  }
}

// ─── PLAYBACK MANIFEST GENERATOR ─────────────────────────────────────────────
/**
 * Runs the complete cascade loop server-side.
 * Returns a Playback Manifest — the client plays this back like a movie.
 */
function generateManifest(params) {
  const {
    serverSeed, clientSeed, nonce,
    bet, weights, rows, cols,
    startVoltMult = 1,
    multiChaser   = false,
    ticketId,
  } = params;

  const size      = rows * cols;
  let   grid      = generateGrid(serverSeed, clientSeed, nonce, weights, rows, cols);
  const initialGrid = Array.from(grid);

  let voltMult  = startVoltMult;
  let totalWin  = 0;
  let sequence  = 0;
  let chain     = 100; // refill nonce offset
  const cascades = [];

  while (true) {
    const clusters = findClusters(grid, rows, cols);
    if (clusters.length === 0) break;
    sequence++;

    // Capacitors inside winning clusters → boost voltage first
    const winSet  = new Set(clusters.flatMap(cl => cl.cells));
    const capCells = [];
    for (const i of winSet) if (grid[i] === SYM.CAPACITOR) capCells.push(i);
    const capMults = capCells.map(() => CAPACITOR_VALUES[(Math.random() * CAPACITOR_VALUES.length) | 0]);
    capMults.forEach(m => { voltMult *= m; });
    voltMult += 1;

    // Scatter count before removal (for near-miss detection)
    const scattersBefore = [];
    for (let i = 0; i < size; i++) if (grid[i] === SYM.SCATTER) scattersBefore.push(i);

    // Cluster payouts
    let stepPay = 0;
    const winningClusters = clusters.map(cl => {
      const base   = (PAY[cl.symbolId] || 0) * cl.cells.length * bet;
      const scaled = multiChaser ? base * 0.5 : base;
      const pay    = Math.round(scaled * voltMult * 100) / 100;
      stepPay += pay;
      return {
        symbol_id:   cl.symbolId,
        symbol_name: SYM_NAME[cl.symbolId],
        coords:      cl.cells.map(i => [(i / cols) | 0, i % cols]),
        size:        cl.cells.length,
        payout:      pay,
      };
    });
    totalWin += stepPay;

    // Max win cap
    const capped = totalWin / bet >= MAX_WIN_MULT;
    if (capped) { totalWin = bet * MAX_WIN_MULT; }

    // Black Hole: scrub low-pays from board
    const holes = [];
    for (let i = 0; i < size; i++) if (grid[i] === SYM.BLACKHOLE) holes.push(i);
    const blackholeFired = holes.length > 0 && clusters.length > 0;
    if (blackholeFired) {
      for (let i = 0; i < size; i++) if (LOW_TIER.has(grid[i])) grid[i] = SYM.EMPTY;
    }

    // Remove winners + capacitors
    for (const i of winSet)   grid[i] = SYM.EMPTY;
    for (const i of capCells) grid[i] = SYM.EMPTY;

    // Gravity
    applyGravity(grid, rows, cols);

    // Refill empties from next HMAC chain
    const refillHex   = hmacSHA256(serverSeed, `${clientSeed}:${nonce}:${chain++}`);
    const refillBytes = Buffer.from(refillHex, 'hex');
    const cum   = buildCumulative(weights);
    const total = weights.reduce((a, b) => a + b, 0);
    let   bi    = 0;
    const newSymbols = [];
    for (let i = 0; i < size; i++) {
      if (grid[i] !== SYM.EMPTY) continue;
      if (bi + 4 > refillBytes.length) bi = 0;
      const val = (refillBytes[bi]<<24|refillBytes[bi+1]<<16|refillBytes[bi+2]<<8|refillBytes[bi+3])>>>0;
      bi += 4;
      const s = pickFromCum(cum, val % total);
      grid[i] = s;
      newSymbols.push({ coord: [(i / cols) | 0, i % cols], symbol_id: s, symbol_name: SYM_NAME[s] });
    }

    // Near-miss: exactly 2 scatters were present before this cascade (natural outcome only)
    const nearMiss = scattersBefore.length === 2;

    cascades.push({
      sequence,
      winning_clusters:    winningClusters,
      capacitor_mults:     capMults,
      voltage_multiplier:  voltMult,
      payout:              stepPay,
      total_win_so_far:    Math.round(totalWin * 100) / 100,
      new_symbols_spawned: newSymbols,
      blackhole_fired:     blackholeFired,
      near_miss:           nearMiss,
      next_grid:           Array.from(grid),  // full grid snapshot for deterministic playback
    });

    if (capped) break;
  }

  // Final scatter count
  const finalScatters = [];
  for (let i = 0; i < size; i++) if (grid[i] === SYM.SCATTER) finalScatters.push(i);

  return {
    ticket_id:          ticketId || crypto.randomUUID(),
    nonce,
    wager:              bet,
    rows, cols,
    initial_grid:       initialGrid,   // flat Int32-style array
    cascades,
    total_win:          Math.round(totalWin * 100) / 100,
    net_profit:         Math.round((totalWin - bet) * 100) / 100,
    final_grid:         Array.from(grid),
    final_volt_mult:    voltMult,
    scatter_count:      finalScatters.length,
    bonus_triggered:    finalScatters.length >= 3,
    server_seed_reveal: serverSeed,
    server_seed_hash:   sha256(serverSeed),
  };
}

// ─── MODULE 3: RTP SIMULATION ─────────────────────────────────────────────────
/**
 * runMathSimulation(spinsCount, opts)
 * Bypasses all network/DB layers. Pure math. Maximum speed.
 *
 * CLI: node math.js --simulate 1000000 [volatility]
 */
function runMathSimulation(spinsCount = 1_000_000, opts = {}) {
  const {
    volatility  = 5,
    bet         = 1.00,
    anteBet     = false,
    multiChaser = false,
    bonusBuy    = false,
    verbose     = true,
  } = opts;

  const weights       = buildWeights(volatility, { anteBet, multiChaser, bonusHighPay: bonusBuy });
  const rows = 6, cols = 6;

  let totalWagered    = 0;
  let totalPaid       = 0;
  let hits            = 0;
  let maxMult         = 0;
  let bonusTriggered  = 0;

  const buckets = { '0x':0, '<1x':0, '1-5x':0, '5-20x':0, '20-100x':0, '100-500x':0, '500x+':0 };
  const startMs = Date.now();

  for (let s = 0; s < spinsCount; s++) {
    // Simulation uses fast random seeds — NOT for production spins
    const serverSeed   = crypto.randomBytes(16).toString('hex');
    const effectiveBet = anteBet ? bet * 1.25 : bet;
    totalWagered      += effectiveBet;

    const manifest = generateManifest({
      serverSeed, clientSeed: 'sim', nonce: s,
      bet: effectiveBet, weights, rows, cols, ticketId: null,
    });

    const win  = manifest.total_win;
    const mult = win / effectiveBet;
    totalPaid += win;
    if (win > 0)    hits++;
    if (mult > maxMult) maxMult = mult;
    if (manifest.bonus_triggered) bonusTriggered++;

    if      (mult === 0)   buckets['0x']++;
    else if (mult < 1)     buckets['<1x']++;
    else if (mult < 5)     buckets['1-5x']++;
    else if (mult < 20)    buckets['5-20x']++;
    else if (mult < 100)   buckets['20-100x']++;
    else if (mult < 500)   buckets['100-500x']++;
    else                   buckets['500x+']++;

    if (verbose && s > 0 && s % 100_000 === 0) {
      process.stdout.write(`\r  ${((s/spinsCount)*100).toFixed(0)}% — RTP: ${((totalPaid/totalWagered)*100).toFixed(3)}%   `);
    }
  }

  const elapsed      = ((Date.now() - startMs) / 1000).toFixed(2);
  const rtp          = (totalPaid / totalWagered) * 100;
  const hitFreq      = (hits / spinsCount) * 100;
  const bonusFreq    = (bonusTriggered / spinsCount) * 100;
  const spinsPerSec  = Math.round(spinsCount / parseFloat(elapsed));

  const report = {
    spins: spinsCount, elapsed_sec: parseFloat(elapsed),
    spins_per_second: spinsPerSec,
    volatility, house_edge: `${(HOUSE_EDGE*100).toFixed(1)}%`,
    rtp_percent:       parseFloat(rtp.toFixed(4)),
    hit_frequency_pct: parseFloat(hitFreq.toFixed(4)),
    max_multiplier:    parseFloat(maxMult.toFixed(2)),
    bonus_trigger_pct: parseFloat(bonusFreq.toFixed(4)),
    total_wagered: parseFloat(totalWagered.toFixed(2)),
    total_paid:    parseFloat(totalPaid.toFixed(2)),
    win_distribution: buckets,
  };

  if (verbose) {
    const pad = (s, n) => String(s).padStart(n);
    console.log('\n\n╔══════════════════════════════════════════╗');
    console.log('║     NEON OVERLOAD — MATH SIMULATION      ║');
    console.log('╠══════════════════════════════════════════╣');
    console.log(`║  Spins:           ${pad(spinsCount.toLocaleString(), 22)} ║`);
    console.log(`║  Speed:           ${pad(spinsPerSec.toLocaleString()+' spins/sec', 22)} ║`);
    console.log(`║  Elapsed:         ${pad(elapsed+'s', 22)} ║`);
    console.log('╠══════════════════════════════════════════╣');
    console.log(`║  RTP:             ${pad(rtp.toFixed(4)+'%', 22)} ║`);
    console.log(`║  Hit Frequency:   ${pad(hitFreq.toFixed(4)+'%', 22)} ║`);
    console.log(`║  Max Multiplier:  ${pad(maxMult.toFixed(2)+'×', 22)} ║`);
    console.log(`║  Bonus Trigger:   ${pad(bonusFreq.toFixed(4)+'%', 22)} ║`);
    console.log(`║  House Edge Cfg:  ${pad((HOUSE_EDGE*100).toFixed(1)+'%', 22)} ║`);
    console.log('╠══════════════════════════════════════════╣');
    console.log('║  WIN DISTRIBUTION                        ║');
    for (const [k, v] of Object.entries(buckets)) {
      const pct = ((v/spinsCount)*100).toFixed(2)+'%';
      console.log(`║  ${k.padEnd(10)} ${pad(pct,8)}  ${pad(v.toLocaleString(),10)} ║`);
    }
    console.log('╚══════════════════════════════════════════╝\n');
  }
  return report;
}

// ─── CLI ──────────────────────────────────────────────────────────────────────
if (require.main === module) {
  const args = process.argv.slice(2);
  if (args[0] === '--simulate') {
    const count = parseInt(args[1]) || 100_000;
    const vol   = parseInt(args[2]) || 5;
    console.log(`\nRunning ${count.toLocaleString()} spin simulation at volatility ${vol}...\n`);
    runMathSimulation(count, { volatility: vol, verbose: true });
  } else {
    console.log('Usage: node math.js --simulate <spins> [volatility 1-10]');
    console.log('       node math.js --simulate 1000000 7');
  }
}

module.exports = {
  SYM, SYM_NAME, PAY, HOUSE_EDGE, MIN_CLUSTER, MAX_WIN_MULT,
  buildWeights, generateGrid, findClusters, applyGravity,
  generateManifest, runMathSimulation,
  hmacSHA256, sha256,
};
