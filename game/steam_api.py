"""
Steamworks integration — silently disabled when the SDK isn't present
or when no Steam client is running (dev / non-Steam launch).
"""

# Map friendly keys → Steamworks achievement API IDs.
# Register these in the Steamworks partner portal under your App ID.
ACHIEVEMENTS = {
    "FIRST_RIP":    "ACH_FIRST_RIP",     # open any pack
    "BIG_HIT":      "ACH_BIG_HIT",       # pull Autograph or higher
    "IMMORTAL":     "ACH_IMMORTAL",      # pull Immortal 1-of-1
    "GEM_MINT":     "ACH_GEM_MINT",      # get PSA 10 grade
    "HIGH_ROLLER":  "ACH_HIGH_ROLLER",   # open a Jumbo Premium pack
    "COLLECTOR_50": "ACH_COLLECTOR_50",  # own 50 cards simultaneously
}


class _SteamAPI:
    def __init__(self):
        self._sdk = None
        self._ok = False

    def init(self):
        try:
            import steamworks  # pip install steamworks
            self._sdk = steamworks.STEAMWORKS()
            self._sdk.initialize()
            self._ok = True
            print("[Steam] Initialized successfully.")
        except Exception as exc:
            print(f"[Steam] Not available ({exc}). Running without Steam features.")

    def steam_name(self) -> str | None:
        if not self._ok:
            return None
        try:
            return self._sdk.Friends.GetPersonaName()
        except Exception:
            return None

    def unlock(self, key: str):
        if not self._ok or key not in ACHIEVEMENTS:
            return
        try:
            self._sdk.UserStats.SetAchievement(ACHIEVEMENTS[key])
            self._sdk.UserStats.StoreStats()
        except Exception:
            pass

    def run_callbacks(self):
        """Call once per frame — required for Steam overlay and achievements."""
        if self._ok:
            try:
                self._sdk.run_callbacks()
            except Exception:
                pass

    def shutdown(self):
        if self._ok and self._sdk:
            try:
                self._sdk.unload()
            except Exception:
                pass


steam = _SteamAPI()
