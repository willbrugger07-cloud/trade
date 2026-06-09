# -*- mode: python ; coding: utf-8 -*-
"""
PyInstaller spec for Sports Card Simulator.

Build (from repo root):
  Windows : build\build_steam.bat
  Mac/Linux: build/build_steam.sh

Output → dist/SportsCardSimulator/
"""
import sys
from pathlib import Path

ROOT = Path(SPECPATH).parent   # repo root

a = Analysis(
    [str(ROOT / "game" / "main.py")],
    pathex=[str(ROOT)],
    binaries=[
        # Drop the Steamworks SDK library next to this spec before building.
        # Uncomment the line matching your target platform:
        # (str(ROOT / "steam_api.dll"),         "."),   # Windows
        # (str(ROOT / "libsteam_api.so"),        "."),   # Linux
        # (str(ROOT / "libsteam_api.dylib"),     "."),   # macOS
    ],
    datas=[
        (str(ROOT / "game" / "assets"), "game/assets"),
    ],
    hiddenimports=[
        "sqlalchemy.dialects.sqlite",
        "sqlalchemy.orm",
        "card_simulator.engine",
        "card_simulator.models",
        "card_simulator.grading",
        "platformdirs",
        "pygame",
    ],
    hookspath=[],
    runtime_hooks=[],
    excludes=["tkinter", "matplotlib", "numpy", "pandas"],
    noarchive=False,
)

pyz = PYZ(a.pure, a.zipped_data)

exe = EXE(
    pyz,
    a.scripts,
    [],
    exclude_binaries=True,
    name="SportsCardSimulator",
    debug=False,
    strip=False,
    upx=True,
    console=False,           # No terminal window on Windows / macOS
    # icon=str(ROOT / "game" / "assets" / "icon.ico"),  # add your icon here
)

coll = COLLECT(
    exe,
    a.binaries,
    a.zipfiles,
    a.datas,
    strip=False,
    upx=True,
    name="SportsCardSimulator",
)
