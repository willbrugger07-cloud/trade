import pygame

from .base import Screen, STAY
from ..ui.button import Button
from ..settings import ACCENT, BG, PANEL, PANEL_BORDER, TEXT, TEXT_DIM


class LoginScreen(Screen):
    def __init__(self, surface, state):
        super().__init__(surface, state)

        # Prefer Steam persona name — auto-login when available
        from ..steam_api import steam
        steam_name = steam.steam_name()

        self._name = steam_name or ""
        self._active = steam_name is None
        self._error = ""
        self._auto = steam_name is not None

        cx = self.W // 2
        self._btn = Button((cx - 85, 390, 170, 48), "PLAY", color=ACCENT)

    # ------------------------------------------------------------------

    def handle_events(self, events):
        from .menu import MenuScreen

        if self._auto:
            self._auto = False
            self.state.load_user(self._name)
            return (MenuScreen, {})

        mouse = pygame.mouse.get_pos()
        self._btn.update(mouse)

        for event in events:
            if self._btn.handle_event(event):
                return self._try_login()

            if event.type == pygame.KEYDOWN and self._active:
                if event.key == pygame.K_RETURN:
                    return self._try_login()
                elif event.key == pygame.K_BACKSPACE:
                    self._name = self._name[:-1]
                    self._error = ""
                elif event.unicode.isprintable() and len(self._name) < 24:
                    self._name += event.unicode
                    self._error = ""

        return STAY

    def _try_login(self):
        from .menu import MenuScreen
        name = self._name.strip()
        if len(name) < 2:
            self._error = "Username must be at least 2 characters."
            return STAY
        self.state.load_user(name)
        return (MenuScreen, {})

    # ------------------------------------------------------------------

    def draw(self):
        self.surface.fill(BG)
        cx = self.W // 2

        tf = pygame.font.SysFont("segoeui", 54, bold=True)
        sf = pygame.font.SysFont("segoeui", 20)
        lf = pygame.font.SysFont("segoeui", 15)
        if_ = pygame.font.SysFont("segoeui", 22)

        title = tf.render("Sports Card Simulator", True, TEXT)
        self.surface.blit(title, title.get_rect(centerx=cx, y=110))

        sub = sf.render("2026 Edition", True, ACCENT)
        self.surface.blit(sub, sub.get_rect(centerx=cx, y=178))

        label = lf.render("Enter Username", True, TEXT_DIM)
        self.surface.blit(label, label.get_rect(centerx=cx, y=280))

        box = pygame.Rect(cx - 170, 302, 340, 50)
        pygame.draw.rect(self.surface, PANEL, box, border_radius=8)
        bc = ACCENT if self._active else PANEL_BORDER
        pygame.draw.rect(self.surface, bc, box, 2, border_radius=8)

        inp = if_.render(self._name, True, TEXT)
        self.surface.blit(inp, inp.get_rect(center=box.center))

        if self._error:
            err = lf.render(self._error, True, (220, 58, 58))
            self.surface.blit(err, err.get_rect(centerx=cx, y=362))

        self._btn.draw(self.surface)

        hint = lf.render("F11 — toggle fullscreen", True, TEXT_DIM)
        self.surface.blit(hint, hint.get_rect(centerx=cx, y=self.H - 30))
