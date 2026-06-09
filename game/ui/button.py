import pygame
from ..settings import ACCENT, TEXT, PANEL, PANEL_BORDER


class Button:
    def __init__(self, rect, text: str, color=None, text_color=None, radius=8):
        self.rect = pygame.Rect(rect)
        self.text = text
        self.color = color or ACCENT
        self.text_color = text_color or TEXT
        self.radius = radius
        self.hovered = False
        self.enabled = True

    def handle_event(self, event) -> bool:
        """Returns True on left-click while enabled."""
        if not self.enabled:
            return False
        if event.type == pygame.MOUSEBUTTONDOWN and event.button == 1:
            return self.rect.collidepoint(event.pos)
        return False

    def update(self, mouse_pos):
        self.hovered = self.enabled and self.rect.collidepoint(mouse_pos)

    def draw(self, surface: pygame.Surface):
        if not self.enabled:
            color = tuple(max(0, c - 40) for c in self.color)
        elif self.hovered:
            color = tuple(min(255, c + 28) for c in self.color)
        else:
            color = self.color

        pygame.draw.rect(surface, color, self.rect, border_radius=self.radius)
        pygame.draw.rect(surface, PANEL_BORDER, self.rect, 1, border_radius=self.radius)

        font = pygame.font.SysFont("segoeui", 19, bold=True)
        label = font.render(self.text, True, self.text_color if self.enabled else TEXT)
        surface.blit(label, label.get_rect(center=self.rect.center))
