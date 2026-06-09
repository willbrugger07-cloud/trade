"""
Headless screenshot script — drives each screen and saves PNGs.
Run under Xvfb:  xvfb-run -a python screenshot_game.py
"""
import os, sys, time
os.environ.setdefault("SDL_VIDEODRIVER", "x11")

sys.path.insert(0, "/home/user/trade")
import pygame

from game.settings import WIDTH, HEIGHT
from game.state import GameState
from game.steam_api import steam
from game.screens.login import LoginScreen
from game.screens.menu import MenuScreen
from game.screens.pack_open import PackOpenScreen
from game.screens.binder import BinderScreen

OUT = "/tmp/screens"
os.makedirs(OUT, exist_ok=True)

pygame.init()
pygame.font.init()
surface = pygame.display.set_mode((WIDTH, HEIGHT))
pygame.display.set_caption("screenshot")

state = GameState()
steam.init()

def save(name):
    path = f"{OUT}/{name}.png"
    pygame.image.save(surface, path)
    print(f"Saved {path}")

def tick(screen, n=2):
    """Advance n frames."""
    for _ in range(n):
        screen.update(1/60)
        screen.draw()
        pygame.display.flip()
        pygame.event.pump()

# ── 1. Login screen ──────────────────────────────────────────────────
login = LoginScreen(surface, state)
login._auto = False          # prevent instant skip
login._name = "Player1"
tick(login, 3)
save("01_login")

# ── 2. Menu screen ────────────────────────────────────────────────────
state.load_user("Player1")
menu = MenuScreen(surface, state)
tick(menu, 3)
save("02_menu")

# ── 3. Pack-open — confirm view ───────────────────────────────────────
pop = PackOpenScreen(surface, state, pack_name="Hobby")
tick(pop, 3)
save("03_pack_confirm")

# ── 4. Pack-open — mid-reveal ─────────────────────────────────────────
pop._do_open()                # rip the pack (bypasses cost check, uses balance)
# advance until all flips have started
for _ in range(200):
    pop.update(1/60)
    pop.draw()
    pygame.display.flip()
    pygame.event.pump()
    if pop._phase == "done":
        break
save("04_pack_reveal")

# ── 5. Binder ─────────────────────────────────────────────────────────
binder = BinderScreen(surface, state)
tick(binder, 5)
save("05_binder")

# ── 6. Binder — card selected ─────────────────────────────────────────
if binder._widgets:
    _, first_card = binder._widgets[0]
    binder._sel_id = first_card.id
tick(binder, 3)
save("06_binder_selected")

state.close()
pygame.quit()
print("Done.")
