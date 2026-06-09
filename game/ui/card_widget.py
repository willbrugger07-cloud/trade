"""
Renders any card-like object (CardResult or ORM Card) as a Pygame surface.
Uses getattr so it works with both without importing either explicitly.
"""
import math

import pygame

from ..settings import (
    CARD_H, CARD_RADIUS, CARD_W, GOLD, TEXT, TEXT_DIM, TIER_BG,
    TIER_COLORS, WHITE,
)

_BIG_HIT_TIERS = {"Autograph", "Patch Auto", "Immortal 1-of-1"}
_FLIP_SPEED = 2.2   # full flips per second


class CardWidget:
    _BACK_COLOR   = (24, 24, 44)
    _BACK_PATTERN = (34, 34, 62)

    def __init__(self, card_data, x: int, y: int, *, face_down: bool = False):
        self.card = card_data
        self.rect = pygame.Rect(x, y, CARD_W, CARD_H)
        self.face_down = face_down

        self._flip_t = 0.0       # 0 → 1 during flip animation
        self._flipping = False
        self._pulse = 0.0        # drives glow sine wave

        self._face_surf = self._render_face()

    # ------------------------------------------------------------------
    # Public API
    # ------------------------------------------------------------------

    def start_flip(self):
        if self.face_down:
            self._flipping = True
            self._flip_t = 0.0

    def update(self, dt: float):
        if self._flipping:
            self._flip_t = min(1.0, self._flip_t + dt * _FLIP_SPEED)
            if self._flip_t >= 1.0:
                self._flipping = False
                self.face_down = False
                self._face_surf = self._render_face()

        if not self.face_down and self._is_big_hit():
            self._pulse = (self._pulse + dt * 2.2) % (2 * math.pi)

    def draw(self, surface: pygame.Surface, *, offset_x: int = 0, offset_y: int = 0):
        x = self.rect.x + offset_x
        y = self.rect.y + offset_y

        if self._flipping:
            t = self._flip_t
            if t < 0.5:
                w = max(1, int(CARD_W * (1.0 - t * 2)))
                src = self._render_back()
            else:
                w = max(1, int(CARD_W * ((t - 0.5) * 2)))
                src = self._face_surf
            scaled = pygame.transform.scale(src, (w, CARD_H))
            surface.blit(scaled, (x + (CARD_W - w) // 2, y))
            return

        if self.face_down:
            surface.blit(self._render_back(), (x, y))
            return

        # Glow for big hits
        if self._is_big_hit():
            alpha = int(55 + 38 * math.sin(self._pulse))
            tier = getattr(self.card, "tier", "Base")
            gc = TIER_COLORS.get(tier, (255, 255, 255))
            glow = pygame.Surface((CARD_W + 16, CARD_H + 16), pygame.SRCALPHA)
            pygame.draw.rect(
                glow, (*gc, alpha),
                (0, 0, CARD_W + 16, CARD_H + 16),
                border_radius=CARD_RADIUS + 4,
            )
            surface.blit(glow, (x - 8, y - 8))

        surface.blit(self._face_surf, (x, y))

    # ------------------------------------------------------------------
    # Helpers
    # ------------------------------------------------------------------

    def _is_big_hit(self) -> bool:
        return getattr(self.card, "tier", "") in _BIG_HIT_TIERS

    def _render_back(self) -> pygame.Surface:
        surf = pygame.Surface((CARD_W, CARD_H), pygame.SRCALPHA)
        pygame.draw.rect(surf, self._BACK_COLOR, (0, 0, CARD_W, CARD_H), border_radius=CARD_RADIUS)
        for row in range(0, CARD_H, 18):
            for col in range(0, CARD_W, 18):
                if (row // 18 + col // 18) % 2 == 0:
                    pygame.draw.rect(surf, self._BACK_PATTERN, (col, row, 16, 16))
        pygame.draw.rect(surf, (55, 55, 95), (0, 0, CARD_W, CARD_H), 2, border_radius=CARD_RADIUS)
        return surf

    def _render_face(self) -> pygame.Surface:
        tier   = getattr(self.card, "tier",         "Base")
        player = getattr(self.card, "player",       "Unknown")
        sport  = getattr(self.card, "sport",        "")
        ovr    = getattr(self.card, "base_rating",  0)
        val    = getattr(self.card, "market_value", 0.0)
        serial = getattr(self.card, "serial_number", None)
        grade  = getattr(self.card, "grade",        None)
        g_val  = getattr(self.card, "graded_value", None)

        display_val = g_val if g_val is not None else val
        bg     = TIER_BG.get(tier,    (18, 18, 32))
        border = TIER_COLORS.get(tier, (80, 80, 115))

        surf = pygame.Surface((CARD_W, CARD_H), pygame.SRCALPHA)
        pygame.draw.rect(surf, bg, (0, 0, CARD_W, CARD_H), border_radius=CARD_RADIUS)
        pygame.draw.rect(surf, border, (0, 0, CARD_W, CARD_H), 2, border_radius=CARD_RADIUS)

        # Tier stripe
        pygame.draw.rect(surf, border, (0, 0, CARD_W, 26),
                         border_top_left_radius=CARD_RADIUS, border_top_right_radius=CARD_RADIUS)
        tf = pygame.font.SysFont("segoeui", 10, bold=True)
        ts = tf.render(tier.upper(), True, WHITE)
        surf.blit(ts, ts.get_rect(center=(CARD_W // 2, 13)))

        # Player name (two lines if needed)
        nf = pygame.font.SysFont("segoeui", 13, bold=True)
        words = player.split()
        mid = max(1, len(words) // 2)
        l1 = nf.render(" ".join(words[:mid]), True, TEXT)
        l2 = nf.render(" ".join(words[mid:]), True, TEXT) if words[mid:] else None
        surf.blit(l1, l1.get_rect(centerx=CARD_W // 2, y=32))
        if l2:
            surf.blit(l2, l2.get_rect(centerx=CARD_W // 2, y=48))

        # OVR (large centrepiece)
        of = pygame.font.SysFont("segoeui", 50, bold=True)
        os_ = of.render(str(ovr), True, border)
        surf.blit(os_, os_.get_rect(center=(CARD_W // 2, 118)))

        lbf = pygame.font.SysFont("segoeui", 10)
        lb  = lbf.render("OVR", True, TEXT_DIM)
        surf.blit(lb, lb.get_rect(center=(CARD_W // 2, 148)))

        # Sport
        sf = pygame.font.SysFont("segoeui", 11)
        ss = sf.render(sport, True, TEXT_DIM)
        surf.blit(ss, ss.get_rect(centerx=CARD_W // 2, y=160))

        # Market / graded value
        vf = pygame.font.SysFont("segoeui", 13, bold=True)
        vs = vf.render(f"${display_val:.2f}", True, GOLD)
        surf.blit(vs, vs.get_rect(centerx=CARD_W // 2, y=177))

        # Serial number
        if serial:
            serf = pygame.font.SysFont("segoeui", 10)
            sers = serf.render(serial, True, TEXT_DIM)
            surf.blit(sers, sers.get_rect(right=CARD_W - 5, bottom=CARD_H - 5))

        # PSA grade badge
        if grade is not None:
            br = pygame.Rect(4, CARD_H - 24, 48, 18)
            pygame.draw.rect(surf, GOLD, br, border_radius=4)
            gf = pygame.font.SysFont("segoeui", 10, bold=True)
            gs = gf.render(f"PSA {grade:.1f}", True, (8, 8, 8))
            surf.blit(gs, gs.get_rect(center=br.center))

        return surf
