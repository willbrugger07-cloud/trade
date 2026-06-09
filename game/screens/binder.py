"""
Collection binder screen — scrollable card grid.
Click a card to select it, then sell or submit for grading.
"""
from datetime import datetime, timezone

import pygame

from .base import Screen, STAY
from ..ui.button import Button
from ..ui.card_widget import CardWidget
from ..settings import (
    ACCENT, BG, CARD_H, CARD_W, GOLD, GREEN, PANEL, PANEL_BORDER,
    RED_COLOR, TEXT, TEXT_DIM,
)
from ..steam_api import steam
from card_simulator.grading import GRADING_FEE, grade_card
from card_simulator.models import Card

_COLS       = 5
_SPACING_X  = CARD_W + 18
_SPACING_Y  = CARD_H + 28
_START_X    = 38
_START_Y    = 90
_SCROLL_SPD = 38


class BinderScreen(Screen):
    def __init__(self, surface, state):
        super().__init__(surface, state)
        state.refresh_user()

        self._scroll   = 0
        self._max_sc   = 0
        self._widgets: list[tuple[CardWidget, object]] = []
        self._sel_id: str | None = None
        self._msg      = ""
        self._msg_t    = 0.0

        self._back_btn  = Button((18, 18, 105, 36),              "← Back",                color=(48, 48, 80))
        self._sell_btn  = Button((self.W - 222, self.H - 58, 95, 40), "Sell",             color=(185, 45, 45))
        self._grade_btn = Button((self.W - 118, self.H - 58, 112, 40), f"Grade (${GRADING_FEE:.0f})", color=(38, 138, 205))

        self._rebuild()

    # ------------------------------------------------------------------

    def _rebuild(self):
        user = self.state.user
        self._widgets = []

        for i, card in enumerate(user.cards):
            col, row = i % _COLS, i // _COLS
            x = _START_X + col * _SPACING_X
            y = _START_Y  + row * _SPACING_Y
            self._widgets.append((CardWidget(card, x, y, face_down=False), card))

        total_rows  = max(1, (len(user.cards) + _COLS - 1) // _COLS)
        content_h   = _START_Y + total_rows * _SPACING_Y
        self._max_sc = max(0, content_h - (self.H - 80))

    # ------------------------------------------------------------------

    def handle_events(self, events):
        from .menu import MenuScreen

        mouse = pygame.mouse.get_pos()
        self._back_btn.update(mouse)

        for event in events:
            if self._back_btn.handle_event(event):
                return (MenuScreen, {})

            if event.type == pygame.MOUSEWHEEL:
                self._scroll = max(0, min(self._max_sc, self._scroll - event.y * _SCROLL_SPD))

            if event.type == pygame.MOUSEBUTTONDOWN and event.button == 1:
                # Map click into scrolled space
                adj = (event.pos[0], event.pos[1] + self._scroll)
                hit = None
                for widget, card in self._widgets:
                    if widget.rect.collidepoint(adj):
                        hit = card.id
                        break
                # Toggle selection
                self._sel_id = None if hit == self._sel_id else hit

            if self._sel_id:
                self._sell_btn.update(mouse)
                self._grade_btn.update(mouse)
                if self._sell_btn.handle_event(event):
                    self._sell()
                if self._grade_btn.handle_event(event):
                    self._grade()

        return STAY

    # ------------------------------------------------------------------

    def _sell(self):
        db   = self.state.db
        user = self.state.user
        card = db.query(Card).filter(Card.id == self._sel_id, Card.owner_id == user.id).first()
        if not card:
            return
        price = card.graded_value if card.graded_value is not None else card.market_value
        user.balance = round(user.balance + price, 2)
        db.delete(card)
        db.commit()
        db.refresh(user)
        self._flash(f"Sold for ${price:.2f}!  Balance: ${user.balance:.2f}", 3.0)
        self._sel_id = None
        self._rebuild()

    def _grade(self):
        db   = self.state.db
        user = self.state.user

        if user.balance < GRADING_FEE:
            self._flash(f"Need ${GRADING_FEE:.0f} to grade.  Have ${user.balance:.2f}", 3.0)
            return

        card = db.query(Card).filter(Card.id == self._sel_id, Card.owner_id == user.id).first()
        if not card:
            return
        if card.grade is not None:
            self._flash(f"Already graded — PSA {card.grade:.1f}", 2.5)
            return

        result = grade_card(card.market_value)
        user.balance      = round(user.balance - GRADING_FEE, 2)
        card.grade        = result.final_grade
        card.graded_value = result.graded_value
        card.graded_at    = datetime.now(timezone.utc)
        db.commit()
        db.refresh(user)

        if result.final_grade == 10.0:
            steam.unlock("GEM_MINT")

        self._flash(
            f"PSA {result.final_grade:.1f} — new value ${result.graded_value:.2f}   "
            f"(×{result.multiplier})   Balance: ${user.balance:.2f}",
            4.5,
        )
        self._sel_id = None
        self._rebuild()

    def _flash(self, msg: str, duration: float):
        self._msg   = msg
        self._msg_t = duration

    # ------------------------------------------------------------------

    def update(self, dt: float):
        if self._msg_t > 0:
            self._msg_t -= dt
        for w, _ in self._widgets:
            w.update(dt)

    # ------------------------------------------------------------------

    def draw(self):
        self.surface.fill(BG)
        cx = self.W // 2

        tf  = pygame.font.SysFont("segoeui", 22, bold=True)
        inf = pygame.font.SysFont("segoeui", 17)
        hf  = pygame.font.SysFont("segoeui", 15)

        user = self.state.user

        title = tf.render(f"📁  Binder — {len(user.cards)} Cards", True, TEXT)
        self.surface.blit(title, title.get_rect(centerx=cx, y=26))

        bal = inf.render(f"💰 ${user.balance:.2f}", True, GOLD)
        self.surface.blit(bal, bal.get_rect(right=self.W - 18, y=28))

        # --- Scrollable card area ---
        clip = pygame.Rect(0, 72, self.W, self.H - 78)
        self.surface.set_clip(clip)

        for widget, card in self._widgets:
            if card.id == self._sel_id:
                hl = pygame.Surface((CARD_W + 8, CARD_H + 8), pygame.SRCALPHA)
                pygame.draw.rect(hl, (75, 138, 255, 115),
                                 (0, 0, CARD_W + 8, CARD_H + 8), border_radius=14)
                self.surface.blit(hl, (widget.rect.x - 4, widget.rect.y - self._scroll - 4))
            widget.draw(self.surface, offset_y=-self._scroll)

        self.surface.set_clip(None)

        # --- Bottom bar ---
        pygame.draw.rect(self.surface, PANEL, (0, self.H - 74, self.W, 74))
        pygame.draw.line(self.surface, PANEL_BORDER, (0, self.H - 74), (self.W, self.H - 74))

        if self._sel_id:
            self._sell_btn.draw(self.surface)
            self._grade_btn.draw(self.surface)
            hint = hf.render("Click again to deselect", True, TEXT_DIM)
            self.surface.blit(hint, hint.get_rect(centerx=cx, centery=self.H - 40))
        elif not user.cards:
            empty = inf.render("Your binder is empty — open some packs!", True, TEXT_DIM)
            self.surface.blit(empty, empty.get_rect(centerx=cx, centery=self.H // 2))
            hint = hf.render("Click a card to select  •  Scroll wheel to browse", True, TEXT_DIM)
            self.surface.blit(hint, hint.get_rect(centerx=cx, centery=self.H - 42))
        else:
            hint = hf.render("Click a card to select  •  Scroll wheel to browse", True, TEXT_DIM)
            self.surface.blit(hint, hint.get_rect(centerx=cx, centery=self.H - 42))

        # Flash message
        if self._msg and self._msg_t > 0:
            mf  = pygame.font.SysFont("segoeui", 16, bold=True)
            ms  = mf.render(self._msg, True, GREEN)
            self.surface.blit(ms, ms.get_rect(centerx=cx, y=self.H - 70))

        self._back_btn.draw(self.surface)
