'use strict';
/**
 * MATH BOOK GENERATOR
 * Pre-calculates N outcome manifests and writes them to compressed lookup files.
 * The Math Service reads from books instead of computing live — sub-millisecond lookups.
 *
 * Usage:
 *   node services/book-generator.js --vol 5 --count 100000 --out books/
 *   node services/book-generator.js --all   (generates books for vol 1,5,10)
 *
 * Output: books/vol-{v}-{i}.json.gz  (gzip; swap zlib for @mongodb/zstd in production)
 *
 * Book index file: books/index-vol-{v}.json
 *   { vol, count, generatedAt, entries: [ { nonce, serverSeedHash, totalWin, scatterCount } ] }
 * Allows the math service to quickly find a valid book entry for a given nonce range.
 */

const crypto = require('crypto');
const zlib   = require('zlib');
const fs     = require('fs');
const path   = require('path');
const { promisify } = require('util');

const gzip   = promisify(zlib.gzip);
const gunzip = promisify(zlib.gunzip);

const { buildWeights, generateManifest, sha256 } = require('../engine/math');

const BOOKS_DIR = path.join(__dirname, '..', 'books');

// ─── GENERATE A SINGLE BOOK ENTRY ────────────────────────────────────────────
function generateEntry(volatility, nonce, opts = {}) {
  const serverSeed = crypto.randomBytes(32).toString('hex');
  const clientSeed = 'BOOK'; // fixed client seed for pre-computed books
  const rows = volatility >= 8 ? 4 : 6;
  const cols = rows;
  const weights = buildWeights(volatility, opts);

  const manifest = generateManifest({
    serverSeed, clientSeed, nonce,
    bet: 1.00,  // normalized to 1x bet; client scales by actual bet at runtime
    weights, rows, cols,
    ticketId: null,
  });

  return {
    nonce,
    serverSeed,
    serverSeedHash: sha256(serverSeed),
    clientSeed,
    volatility,
    // Store full manifest compressed; index stores only summary
    manifest,
    summary: {
      nonce,
      serverSeedHash: sha256(serverSeed),
      totalWin:       manifest.total_win,
      cascades:       manifest.cascades.length,
      scatterCount:   manifest.scatter_count,
      bonusTriggered: manifest.bonus_triggered,
      finalVoltMult:  manifest.final_volt_mult,
    },
  };
}

// ─── WRITE COMPRESSED BOOK FILE ───────────────────────────────────────────────
async function writeBookEntry(entry, bookDir) {
  const filePath = path.join(bookDir, `${entry.nonce}.json.gz`);
  const json     = JSON.stringify(entry.manifest);
  const compressed = await gzip(json, { level: 9 });
  fs.writeFileSync(filePath, compressed);
  return filePath;
}

// ─── READ BOOK ENTRY ──────────────────────────────────────────────────────────
async function readBookEntry(volatility, nonce) {
  const bookDir = path.join(BOOKS_DIR, `vol-${volatility}`);
  const filePath = path.join(bookDir, `${nonce}.json.gz`);
  if (!fs.existsSync(filePath)) return null;
  const compressed = fs.readFileSync(filePath);
  const json = await gunzip(compressed);
  return JSON.parse(json.toString());
}

// ─── GET NEXT AVAILABLE NONCE FROM BOOK ───────────────────────────────────────
function getBookIndex(volatility) {
  const indexPath = path.join(BOOKS_DIR, `vol-${volatility}`, 'index.json');
  if (!fs.existsSync(indexPath)) return null;
  return JSON.parse(fs.readFileSync(indexPath, 'utf8'));
}

function pickBookEntry(volatility, nonce) {
  // Map nonce to a book entry using modulo (books are finite, reuse cyclically)
  const index = getBookIndex(volatility);
  if (!index || !index.count) return null;
  return nonce % index.count; // returns the book nonce to look up
}

// ─── GENERATE BOOK ────────────────────────────────────────────────────────────
async function generateBook(volatility, count, opts = {}) {
  const { verbose = true, outDir = BOOKS_DIR } = opts;
  const bookDir = path.join(outDir, `vol-${volatility}`);
  fs.mkdirSync(bookDir, { recursive: true });

  const index = {
    vol:         volatility,
    count,
    generatedAt: new Date().toISOString(),
    entries:     [],
  };

  const startMs = Date.now();
  let   written = 0;

  if (verbose) process.stdout.write(`\nGenerating ${count.toLocaleString()} book entries (vol ${volatility})...\n`);

  for (let nonce = 0; nonce < count; nonce++) {
    const entry = generateEntry(volatility, nonce, opts);
    await writeBookEntry(entry, bookDir);
    index.entries.push(entry.summary);
    written++;

    if (verbose && written % 1000 === 0) {
      const pct     = ((written / count) * 100).toFixed(1);
      const elapsed = ((Date.now() - startMs) / 1000).toFixed(1);
      process.stdout.write(`\r  ${pct}% (${written.toLocaleString()}/${count.toLocaleString()}) — ${elapsed}s elapsed   `);
    }
  }

  // Write index file
  const indexPath = path.join(bookDir, 'index.json');
  fs.writeFileSync(indexPath, JSON.stringify(index));

  const elapsed   = ((Date.now() - startMs) / 1000).toFixed(2);
  const dirSize   = getDirSizeMB(bookDir);

  if (verbose) {
    console.log(`\n\n╔══════════════════════════════════════════╗`);
    console.log(`║        MATH BOOK GENERATION COMPLETE     ║`);
    console.log(`╠══════════════════════════════════════════╣`);
    console.log(`║  Volatility:  ${String(volatility).padStart(26)} ║`);
    console.log(`║  Entries:     ${count.toLocaleString().padStart(26)} ║`);
    console.log(`║  Elapsed:     ${(elapsed+'s').padStart(26)} ║`);
    console.log(`║  Output dir:  ${bookDir.slice(-26).padStart(26)} ║`);
    console.log(`║  Size on disk:${(dirSize+'MB').padStart(26)} ║`);
    console.log(`╚══════════════════════════════════════════╝\n`);
  }

  return { bookDir, indexPath, count: written, elapsed };
}

function getDirSizeMB(dir) {
  let total = 0;
  try {
    for (const f of fs.readdirSync(dir)) {
      try { total += fs.statSync(path.join(dir, f)).size; } catch {}
    }
  } catch {}
  return (total / 1024 / 1024).toFixed(1);
}

// ─── CLI ──────────────────────────────────────────────────────────────────────
async function main() {
  const args  = process.argv.slice(2);
  const flags = {};
  for (let i = 0; i < args.length; i++) {
    if (args[i].startsWith('--')) flags[args[i].slice(2)] = args[i+1] || true;
  }

  if (flags.all) {
    // Generate books for low, medium, and insane volatility
    for (const vol of [1, 5, 10]) {
      await generateBook(parseInt(vol), parseInt(flags.count) || 10_000, {
        verbose: true, outDir: flags.out || BOOKS_DIR,
      });
    }
    return;
  }

  const vol   = parseInt(flags.vol)   || 5;
  const count = parseInt(flags.count) || 10_000;
  const out   = flags.out             || BOOKS_DIR;

  await generateBook(vol, count, { verbose: true, outDir: out });
}

if (require.main === module) main().catch(console.error);

module.exports = { generateBook, readBookEntry, getBookIndex, pickBookEntry, generateEntry };
