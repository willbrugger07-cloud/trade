"""
Visual constants, color palette, and card geometry for the Pygame frontend.
"""

# Window
WIDTH, HEIGHT = 1280, 720
FPS = 60
TITLE = "Sports Card Simulator 2026"

# Card geometry
CARD_W, CARD_H = 158, 228
CARD_RADIUS = 10

# Tier accent colors (border / badge)
TIER_COLORS = {
    "Base":             (80,  80,  115),
    "Insert":           (35,  95,  210),
    "Refractor":        (35, 175,  125),
    "Autograph":        (205, 168,  25),
    "Patch Auto":       (175,  50, 210),
    "Immortal 1-of-1":  (225,  35,  35),
}

# Card face background tint
TIER_BG = {
    "Base":             (18, 18, 32),
    "Insert":           (12, 28, 58),
    "Refractor":        (12, 48, 38),
    "Autograph":        (48, 38,  5),
    "Patch Auto":       (42,  8, 52),
    "Immortal 1-of-1":  (52,  5,  5),
}

# UI palette
BG           = (10,  10,  18)
PANEL        = (18,  18,  32)
PANEL_BORDER = (48,  48,  88)
TEXT         = (215, 215, 235)
TEXT_DIM     = (105, 105, 135)
ACCENT       = (65,  128, 255)
GREEN        = (48,  195,  65)
RED_COLOR    = (215,  52,  52)
GOLD         = (215, 172,  32)
WHITE        = (255, 255, 255)
BLACK        = (0,   0,   0)
