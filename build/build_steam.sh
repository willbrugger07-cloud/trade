#!/usr/bin/env bash
# ============================================================
# Sports Card Simulator — Steam build script  (Linux / macOS)
# ============================================================
# Prerequisites:
#   pip install pyinstaller
#   (optional) drop libsteam_api.so / .dylib next to this repo root
#
# Usage:
#   cd <repo root>
#   bash build/build_steam.sh
# ============================================================
set -euo pipefail
cd "$(dirname "$0")/.."

echo "=== Installing / verifying dependencies ==="
pip install -r requirements.txt pyinstaller --quiet

echo ""
echo "=== Building with PyInstaller ==="
pyinstaller build/card_simulator.spec \
    --clean \
    --distpath dist \
    --workpath build_tmp \
    --noconfirm

echo ""
echo "=== Build complete! ==="
echo "Output : dist/SportsCardSimulator/"
echo ""
echo "Next steps for Steam:"
echo "  1. Add your App ID to build/steam_appid.txt"
echo "  2. Copy libsteam_api.so (Linux) or libsteam_api.dylib (macOS)"
echo "     into dist/SportsCardSimulator/"
echo "  3. Copy build/steam_appid.txt into dist/SportsCardSimulator/"
echo "  4. Upload the directory via SteamPipe:"
echo "     https://partner.steamgames.com/doc/sdk/uploading"
