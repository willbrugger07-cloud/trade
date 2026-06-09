import pygame

from .base import Screen, STAY
from ..ui.button import Button
from ..settings import ACCENT, BG, GOLD, PANEL, PANEL_BORDER, TEXT, TEXT_DIM


_PACK_DEFS = [
    ("Standard Pack — $10",     "Standard",      (38, 100, 210)),
    ("Hobby Pack — $50",        "Hobby",         (38, 175, 125)),
    ("Jumbo Premium — $120",    "Jumbo Premium", (175, 48, 210)),
]


class MenuScreen(Screen):
    def __init__(self, surface, state):
        super().__init__(surface, state)
        state.refresh_user()

        cx = self.W // 2
        bw, bh, gap = 290, 56, 16
        sy = 285

        self._pack_btns = [
            (Button((cx - bw // 2, sy + i * (bh + gap), bw, bh), label, color=color), name)
            for i, (label, name, color) in enumerate(_PACK_DEFS)
        ]

        by = sy + 3 * (bh + gap) + 24
        self._binder_btn = Button((cx - bw // 2, by,       bw, bh), "📁  View Binder",    color=(48, 48, 88))
        self._exit_btn   = Button((cx - 75,       by + bh + 16, 150, 40), "Exit Game", color=(78, 28, 28))

    # ------------------------------------------------------------------

    def handle_events(self, events):
        from .pack_open import PackOpenScreen
        from .binder import BinderScreen
        import sys

        mouse = pygame.mouse.get_pos()
        for btn, _ in self._pack_btns:
            btn.update(mouse)
        self._binder_btn.update(mouse)
        self._exit_btn.update(mouse)

        for event in events:
            for btn, pack_name in self._pack_btns:
                if btn.handle_event(event):
                    return (PackOpenScreen, {"pack_name": pack_name})
            if self._binder_btn.handle_event(event):
                return (BinderScreen, {})
            if self._exit_btn.handle_event(event):
                from ..steam_api import steam
                steam.shutdown()
                self.state.close()
                pygame.quit()
                sys.exit()

        return STAY

    # ------------------------------------------------------------------

    def draw(self):
        self.surface.fill(BG)
        cx = self.W // 2

        user = self.state.user

        tf = pygame.font.SysFont("segoeui", 38, bold=True)
        inf = pygame.font.SysFont("segoeui", 20)
        lf  = pygame.font.SysFont("segoeui", 15)

        title = tf.render(f"Welcome, {user.username}!", True, TEXT)
        self.surface.blit(title, title.get_rect(centerx=cx, y=75))

        # Stat row
        bal  = inf.render(f"💰  ${user.balance:.2f}", True, GOLD)
        cnt  = inf.render(f"📦  {len(user.cards)} cards", True, TEXT)
        self.surface.blit(bal, bal.get_rect(centerx=cx - 130, y=136))
        self.surface.blit(cnt, cnt.get_rect(centerx=cx + 130, y=136))

        divider = lf.render("— Select a Pack to Open —", True, TEXT_DIM)
        self.surface.blit(divider, divider.get_rect(centerx=cx, y=248))

        for btn, _ in self._pack_btns:
            btn.draw(self.surface)
        self._binder_btn.draw(self.surface)
        self._exit_btn.draw(self.surface)
