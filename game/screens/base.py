from __future__ import annotations
from typing import Any, Optional, Tuple, Type

import pygame

# A screen's handle_events returns (NextScreenClass | None, kwargs dict).
# Return STAY to remain on the current screen.
ScreenTransition = Tuple[Optional[Type["Screen"]], dict[str, Any]]
STAY: ScreenTransition = (None, {})


class Screen:
    def __init__(self, surface: pygame.Surface, state):
        self.surface = surface
        self.state = state
        self.W, self.H = surface.get_size()

    def handle_events(self, events: list) -> ScreenTransition:
        return STAY

    def update(self, dt: float):
        pass

    def draw(self):
        pass
