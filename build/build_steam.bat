@echo off
REM ============================================================
REM  Sports Card Simulator — Steam build script  (Windows)
REM ============================================================
REM Prerequisites:
REM   pip install pyinstaller
REM   (optional) drop steam_api.dll next to repo root
REM
REM Usage: double-click OR run from repo root:
REM   build\build_steam.bat
REM ============================================================

cd /d "%~dp0.."

echo === Installing / verifying dependencies ===
pip install -r requirements.txt pyinstaller --quiet

echo.
echo === Building with PyInstaller ===
pyinstaller build\card_simulator.spec ^
    --clean ^
    --distpath dist ^
    --workpath build_tmp ^
    --noconfirm

echo.
echo === Build complete! ===
echo Output : dist\SportsCardSimulator\
echo.
echo Next steps for Steam:
echo   1. Add your App ID to build\steam_appid.txt
echo   2. Copy steam_api.dll into dist\SportsCardSimulator\
echo   3. Copy build\steam_appid.txt into dist\SportsCardSimulator\
echo   4. Upload via SteamPipe:
echo      https://partner.steamgames.com/doc/sdk/uploading
pause
