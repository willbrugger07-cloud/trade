"""
Pack-opening screen: animated card flip reveal with big-hit fanfare.
"""
import pygame

from .base import Screen, STAY
from ..ui.button import Button
from ..ui.card_widget import CardWidget
from ..settings import (
    ACCENT, BG, CARD_H, CARD_W, GOLD, PANEL, PANEL_BORDER,
    RED_COLOR, TEXT, TEXT_DIM,
)
from ..steam_api import steam
from card_simulator.engine import PACK_TYPES, open_pack
from card_simulator.models import Card

_FLIP_DELAY = 0.28   # seconds between sequential flips
_COLS       = 5


class PackOpenScreen(Screen):
    def __init__(self, surface, state, *, pack_name: str = "Standard"):
        super().__init__(surface, state)
        state.refresh_user()

        self.pack_name = pack_name
        self._phase    = "confirm"   # confirm → reveal → done
        self._error    = ""
        self._widgets: list[CardWidget] = []
        self._flip_queue: list[int]    = []
        self._flip_timer = 0.0

        cost = PACK_TYPES[pack_name]["cost"]
        cx   = self.W // 2

        self._open_btn = Button((cx - 95, 400, 190, 52), f"Rip Pack  (${cost})", color=(38, 165, 78))
        self._back_btn = Button((cx - 60, 468, 120, 40), "← Back",              color=(48, 48, 80))
        self._done_btn = Button((cx - 85, self.H - 72, 170, 48), "Keep Going →", color=ACCENT)

    # ------------------------------------------------------------------

    def _do_open(self):
        user = self.state.user
        db   = self.state.db

        cost = PACK_TYPES[self.pack_name]["cost"]
        if user.balance < cost:
            self._error = f"Insufficient funds — need ${cost:.0f}, have ${user.balance:.2f}"
            return

        cards_pulled, paid = open_pack(self.pack_name)
        user.balance = round(user.balance - paid, 2)

        # Steam achievements
        steam.unlock("FIRST_RIP")
        if self.pack_name == "Jumbo Premium":
            steam.unlock("HIGH_ROLLER")

        n    = len(cards_pulled)
        cols = min(n, _COLS)
        rows = (n + cols - 1) // cols

        sx  = CARD_W + 18
        sy  = CARD_H + 22
        x0  = self.W // 2 - (cols * sx - 18) // 2
        y0  = max(90, self.H // 2 - (rows * sy) // 2 + 20)

        self._widgets = []
        for i, cr in enumerate(cards_pulled):
            col, row = i % cols, i // cols
            widget = CardWidget(cr, x0 + col * sx, y0 + row * sy, face_down=True)
            self._widgets.append(widget)

            # Persist to DB
            db.add(Card(
                id=cr.card_id, owner_id=user.id,
                sport=cr.sport, player=cr.player, tier=cr.tier,
                base_rating=cr.base_rating, market_value=cr.market_value,
                serial_number=cr.serial_number,
            ))

            if cr.is_big_hit:
                steam.unlock("BIG_HIT")
            if cr.tier == "Immortal 1-of-1":
                steam.unlock("IMMORTAL")

        db.commit()
        db.refresh(user)

        if len(user.cards) >= 50:
            steam.unlock("COLLECTOR_50")

        self._flip_queue = list(range(n))
        self._flip_timer = _FLIP_DELAY
        self._phase      = "reveal"

    # ------------------------------------------------------------------

    def handle_events(self, events):
        from .menu import MenuScreen

        mouse = pygame.mouse.get_pos()
        for event in events:
            if self._phase == "confirm":
                self._open_btn.update(mouse)
                self._back_btn.update(mouse)
                if self._open_btn.handle_event(event):
                    self._do_open()
                if self._back_btn.handle_event(event):
                    return (MenuScreen, {})

            elif self._phase == "done":
                self._done_btn.update(mouse)
                if self._done_btn.handle_event(event):
                    return (MenuScreen, {})

        return STAY

    # ------------------------------------------------------------------

    def update(self, dt: float):
        if self._phase not in ("reveal", "done"):
            return

        for w in self._widgets:
            w.update(dt)

        if self._phase == "reveal":
            if self._flip_queue:
                self._flip_timer -= dt
                if self._flip_timer <= 0:
                    self._widgets[self._flip_queue.pop(0)].start_flip()
                    self._flip_timer = _FLIP_DELAY
            elif all(not w._flipping for w in self._widgets):
                self._phase = "done"

    # ------------------------------------------------------------------

    def draw(self):
        self.surface.fill(BG)
        cx = self.W // 2

        tf  = pygame.font.SysFont("segoeui", 26, bold=True)
        inf = pygame.font.SysFont("segoeui", 18)
        df  = pygame.font.SysFont("segoeui", 20)

        title = tf.render(f"Opening: {self.pack_name} Pack", True, TEXT)
        self.surface.blit(title, title.get_rect(centerx=cx, y=28))

        bal = inf.render(f"Balance: ${self.state.user.balance:.2f}", True, GOLD)
        self.surface.blit(bal, bal.get_rect(right=self.W - 24, y=30))

        # ---- CONFIRM phase ----
        if self._phase == "confirm":
            pack = PACK_TYPES[self.pack_name]
            for i, line in enumerate([
                f"Cost    : ${pack['cost']}",
                f"Cards   : {pack['cards']}",
                f"Hit boost: ×{pack['boost']}",
            ]):
                s = df.render(line, True, TEXT)
                self.surface.blit(s, s.get_rect(centerx=cx, y=245 + i * 42))

            if self._error:
                err = df.render(self._error, True, RED_COLOR)
                self.surface.blit(err, err.get_rect(centerx=cx, y=378))

            self._open_btn.draw(self.surface)
            self._back_btn.draw(self.surface)

        # ---- REVEAL / DONE phase ----
        else:
            for w in self._widgets:
                w.draw(self.surface)

            if self._phase == "done":
                self._done_btn.draw(self.surface)
