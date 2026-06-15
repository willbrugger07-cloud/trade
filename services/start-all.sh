#!/bin/bash
# Start all four services. Each runs as an independent process.
# In production: use systemd units, Docker Compose, or Kubernetes pods.

cd "$(dirname "$0")/.."

echo "Installing dependencies..."
cd engine && npm install --silent 2>/dev/null; cd ..

echo ""
echo "Starting Neon Overload microservices..."
echo ""

# Start Ledger Service (port 3002)
LEDGER_PORT=3002 node services/ledger-service.js &
LEDGER_PID=$!

# Start Math Service (port 3001)
MATH_PORT=3001 LEDGER_PORT=3002 node services/math-service.js &
MATH_PID=$!

sleep 1

# Start Wallet Sync Service (port 3003)
SYNC_PORT=3003 LEDGER_PORT=3002 SETTLEMENT_INTERVAL_MS=60000 node services/wallet-sync-service.js &
SYNC_PID=$!

# Start Gateway (port 3000)
PORT=3000 MATH_PORT=3001 LEDGER_PORT=3002 SYNC_PORT=3003 node services/gateway.js &
GATEWAY_PID=$!

echo ""
echo "All services running:"
echo "  Gateway     → http://localhost:3000  (PID $GATEWAY_PID)"
echo "  Math        → http://localhost:3001  (PID $MATH_PID)"
echo "  Ledger      → http://localhost:3002  (PID $LEDGER_PID)"
echo "  Wallet Sync → http://localhost:3003  (PID $SYNC_PID)"
echo ""
echo "Open http://localhost:3000 in your browser."
echo ""
echo "Useful endpoints:"
echo "  GET  /wallet-balances         — Hot/warm/cold balances"
echo "  GET  /settlement-log          — Blockchain settlement history"
echo "  GET  /flagged-accounts        — AML flagged accounts"
echo "  POST /withdraw                — Queue a withdrawal"
echo "  POST /validate-rtp            — Run RTP math simulation"
echo "  GET  /ledger/:accountId       — Full audit trail for account"
echo ""
echo "Press Ctrl+C to stop all services."
echo ""

trap "echo 'Stopping...'; kill $GATEWAY_PID $MATH_PID $LEDGER_PID $SYNC_PID 2>/dev/null; exit 0" INT

wait
