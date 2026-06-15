#!/bin/bash
# Start all three services. Each runs as an independent process.
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

# Start Gateway (port 3000)
PORT=3000 MATH_PORT=3001 LEDGER_PORT=3002 node services/gateway.js &
GATEWAY_PID=$!

echo ""
echo "All services running:"
echo "  Gateway  → http://localhost:3000  (PID $GATEWAY_PID)"
echo "  Math     → http://localhost:3001  (PID $MATH_PID)"
echo "  Ledger   → http://localhost:3002  (PID $LEDGER_PID)"
echo ""
echo "Open http://localhost:3000 in your browser."
echo "Press Ctrl+C to stop all services."
echo ""

# Trap Ctrl+C and kill all services cleanly
trap "echo 'Stopping...'; kill $GATEWAY_PID $MATH_PID $LEDGER_PID 2>/dev/null; exit 0" INT

wait
