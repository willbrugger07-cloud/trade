'use strict';
/**
 * MODULE 1: MATHEMATICS & PROVABLY FAIR ENGINE
 * Pure functions — no I/O, no side effects.
 */

const crypto = require('crypto');

// ─── SYMBOL DEFINITIONS ──────────────────────────────────────────────────────
const SYMBOLS = {
  triangle:  { pay: 0.20, tier: 'low'     },
  square:    { pay: 0.30, tier: 'low'     },
  circle:    { pay: 0.50, tier: 'mid'     },
  diamond:   { pay: 0.80, tier: 'mid'     },
  hexagon:   { pay: 2.00, tier: 'high'    },
  capacitor: { pay: 0,    tier: 'special' },
  wild:      { pay: 0,    tier: 'special' },
  blackhole: { pay: 0,    tier: 'special' },
  scatter:   { pay: 0,    tier: 'special' },
};

const CAPACITOR_VALUES = [2, 5, 10];

// Base weights per volatility band (1-3 low, 4-7 mid, 8-10 insane)
const WEIGHT_PRESETS = {
  low:    { triangle:35, square:35, circle:14, diamond:10, hexagon:3, capacitor:2, wild:1, blackhole:0, scatter:0 },
  medium: { triangle:28, square:27, circle:15, diamond:13, hexagon:8, capacitor:4, wild:3, blackhole:1, scatter:1 },
  high:   { triangle:8,  square:8,  circle:16, diamond:18, hexagon:28, capacitor:10, wild:8, blackhole:2, scatter:2 },
};

const MAX_WIN_MULTIPLIER = 10000;
const MIN_CLUSTER_SIZE   = 5;

// ─── RNG ─────────────────────────────────────────────────────────────────────
function hmacSHA256(serverSeed, message) {
  return crypto.createHmac('sha256', serverSeed).update(message).digest('hex');
}

/**
 * Generate an array of floats [0,1) from a single HMAC result.
 * Each 8-hex-char chunk → one float. Wraps around hex string if count > 8.
 */
function generateFloats(serverSeed, clientSeed, nonce, count) {
  const hex = hmacSHA256(serverSeed, `${clientSeed}:${nonce}`);
  const floats = [];
  for (let i = 0; i < count; i++) {
    const offset = (i * 8) % (hex.length - 7);
    floats.push(parseInt(hex.substr(offset, 8), 16) / 0xffffffff);
  }
  return floats;
}

// ─── WEIGHT HELPERS ──────────────────────────────────────────────────────────
function buildWeights(volatility, opts = {}) {
  let preset;
  if (volatility <= 3) preset = 'low';
  else if (volatility <= 7) preset = 'medium';
  else preset = 'high';

  const w = { ...WEIGHT_PRESETS[preset] };
  if (opts.anteBet)     w.capacitor *= 2;
  if (opts.multiChaser) w.capacitor  = Math.max(w.capacitor, 30);
  if (opts.bonusHighPay) {
    // Prism phase: shift toward higher pays
    w.circle   = Math.round(w.circle   * 1.3);
    w.diamond  = Math.round(w.diamond  * 1.3);
    w.hexagon  = Math.round(w.hexagon  * 1.5);
    w.triangle = Math.max(1, Math.round(w.triangle * 0.5));
    w.square   = Math.max(1, Math.round(w.square   * 0.5));
  }
  return w;
}

function pickSymbol(weights, rng) {
  const total = Object.values(weights).reduce((a, b) => a + b, 0);
  let r = rng * total;
  for (const [sym, wt] of Object.entries(weights)) {
    r -= wt;
    if (r <= 0) return sym;
  }
  return Object.keys(weights)[0];
}

function gridSize(volatility, inPrism) {
  if (inPrism)        return { rows: 8, cols: 8 };
  if (volatility >= 8) return { rows: 4, cols: 4 };
  return { rows: 6, cols: 6 };
}

// ─── GRID POPULATION ─────────────────────────────────────────────────────────
function populateGrid(floats, weights, rows, cols, existing = null) {
  const grid = existing
    ? existing.map(row => [...row])
    : Array.from({ length: rows }, () => Array(cols).fill(null));

  let fi = 0;
  for (let r = 0; r < rows; r++)
    for (let c = 0; c < cols; c++)
      if (grid[r][c] === null) grid[r][c] = pickSymbol(weights, floats[fi++] ?? Math.random());

  return grid;
}

// ─── CLUSTER DETECTION (flood fill) ──────────────────────────────────────────
function findClusters(grid, rows, cols) {
  const visited = Array.from({ length: rows }, () => Array(cols).fill(false));
  const clusters = [];

  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < cols; c++) {
      const sym = grid[r][c];
      if (!sym || visited[r][c] || ['capacitor','blackhole','scatter','wild'].includes(sym)) continue;

      const cells = [];
      const queue = [[r, c]];
      visited[r][c] = true;
      let target = sym;

      while (queue.length) {
        const [cr, cc] = queue.shift();
        cells.push([cr, cc]);
        for (const [nr, nc] of [[cr-1,cc],[cr+1,cc],[cr,cc-1],[cr,cc+1]]) {
          if (nr < 0 || nr >= rows || nc < 0 || nc >= cols || visited[nr][nc]) continue;
          const ns = grid[nr][nc];
          if (!ns || ['capacitor','blackhole','scatter'].includes(ns)) continue;
          if (ns === 'wild' || ns === target) {
            visited[nr][nc] = true;
            queue.push([nr, nc]);
          }
        }
      }

      if (cells.length >= MIN_CLUSTER_SIZE) {
        clusters.push({ symbol: target, cells, size: cells.length });
      }
    }
  }
  return clusters;
}

function findType(grid, rows, cols, type) {
  const found = [];
  for (let r = 0; r < rows; r++)
    for (let c = 0; c < cols; c++)
      if (grid[r][c] === type) found.push([r, c]);
  return found;
}

// ─── PULSE GRAVITY ───────────────────────────────────────────────────────────
// Pack non-null symbols toward bottom of each column; top cells become null.
function applyGravity(grid, rows, cols) {
  for (let c = 0; c < cols; c++) {
    const col = [];
    for (let r = 0; r < rows; r++) if (grid[r][c] !== null) col.push(grid[r][c]);
    const startRow = rows - col.length;
    for (let r = 0; r < rows; r++) grid[r][c] = r < startRow ? null : col[r - startRow];
  }
  return grid;
}

// ─── CASCADE SIMULATION ──────────────────────────────────────────────────────
/**
 * Runs the full cascade loop synchronously. Returns a cascade history array
 * that the client replays as animation frames.
 *
 * @param {object} params
 * @returns {{ steps, totalWin, finalGrid, scatterCount, nearMiss, blackholeFired, cappedAt }}
 */
function simulateCascade(params) {
  const {
    initialGrid, serverSeed, clientSeed, nonce,
    bet, weights, rows, cols,
    voltageMultiplier: startMult = 1,
    multiChaser = false,
  } = params;

  let grid = initialGrid.map(row => [...row]);
  let voltMult = startMult;
  let cascadeCount = 0;
  let totalWin = 0;
  let capped = false;
  const steps = [];
  let nonceOffset = 0;

  while (true) {
    const clusters = findClusters(grid, rows, cols);
    const scatters  = findType(grid, rows, cols, 'scatter');

    // Scatter trigger check handled by caller; here we just record count
    if (clusters.length === 0) break;

    // Capacitors adjacent to winning cells modify voltage first
    const winCellSet = new Set(clusters.flatMap(cl => cl.cells.map(([r,c]) => `${r},${c}`)));
    const caps = findType(grid, rows, cols, 'capacitor').filter(([r,c]) => winCellSet.has(`${r},${c}`));
    const capMults = caps.map(() => CAPACITOR_VALUES[Math.floor(Math.random() * CAPACITOR_VALUES.length)]);
    capMults.forEach(m => { voltMult *= m; });

    cascadeCount++;
    voltMult += 1;

    // Compute round payout
    let roundPay = 0;
    const clusterPayouts = clusters.map(cl => {
      const base = SYMBOLS[cl.symbol].pay * cl.size * bet;
      const scaled = multiChaser ? base * 0.5 : base;
      const pay = scaled * voltMult;
      roundPay += pay;
      return { symbol: cl.symbol, size: cl.size, cells: cl.cells, pay };
    });

    totalWin += roundPay;

    // Black Hole: remove low-pays after win resolution this step
    const holes = findType(grid, rows, cols, 'blackhole');
    let blackholeFired = false;
    if (holes.length && clusters.length > 0) {
      const LOW = new Set(['triangle', 'square']);
      for (let r = 0; r < rows; r++)
        for (let c = 0; c < cols; c++)
          if (LOW.has(grid[r][c])) grid[r][c] = null;
      blackholeFired = true;
    }

    // Record step before removal
    steps.push({
      cascade: cascadeCount,
      clusters: clusterPayouts,
      capMults,
      voltMult,
      roundPay,
      totalWinSoFar: totalWin,
      blackholeFired,
    });

    // Remove winning cells + capacitors
    const removeCells = new Set([...winCellSet, ...caps.map(([r,c]) => `${r},${c}`)]);
    for (const key of removeCells) {
      const [r, c] = key.split(',').map(Number);
      grid[r][c] = null;
    }

    // Cap check
    if (totalWin / bet >= MAX_WIN_MULTIPLIER) {
      totalWin = bet * MAX_WIN_MULTIPLIER;
      capped = true;
      break;
    }

    // Gravity + refill
    applyGravity(grid, rows, cols);
    const floats = generateFloats(serverSeed, clientSeed, nonce + (++nonceOffset), rows * cols);
    grid = populateGrid(floats, weights, rows, cols, grid);
  }

  // Near-miss detection: exactly 2 scatters on final grid (natural outcome only)
  const finalScatters = findType(grid, rows, cols, 'scatter').length;
  const nearMiss = finalScatters === 2;

  return {
    steps,
    totalWin: Math.round(totalWin * 100) / 100,
    finalGrid: grid,
    voltageMultiplier: voltMult,
    cascadeCount,
    nearMiss,
    scatterCount: findType(grid, rows, cols, 'scatter').length,
    cappedAt: capped ? MAX_WIN_MULTIPLIER : null,
  };
}

// ─── SHA-256 HASH (for commitment scheme) ────────────────────────────────────
function sha256(str) {
  return crypto.createHash('sha256').update(str).digest('hex');
}

module.exports = {
  hmacSHA256, generateFloats, buildWeights, pickSymbol, gridSize,
  populateGrid, findClusters, findType, applyGravity, simulateCascade,
  sha256, SYMBOLS, MAX_WIN_MULTIPLIER,
};
