"""
Entry point for the Pygame GUI game.

Run locally  : python -m game.main
Run packaged : dist/SportsCardSimulator/SportsCardSimulator(.exe)
"""
import sys

import pygame

from .settings import FPS, HEIGHT, TITLE, WIDTH
from .state import GameState
from .steam_api import steam
from .screens.login import LoginScreen


def main():
    pygame.init()
    pygame.font.init()

    surface = pygame.display.set_mode((WIDTH, HEIGHT))
    pygame.display.set_caption(TITLE)
    clock = pygame.time.Clock()

    state = GameState()
    steam.init()

    current = LoginScreen(surface, state)

    running = True
    while running:
        dt = clock.tick(FPS) / 1000.0

        events = pygame.event.get()
        for event in events:
            if event.type == pygame.QUIT:
                running = False
            elif event.type == pygame.KEYDOWN and event.key == pygame.K_F11:
                pygame.display.toggle_fullscreen()

        next_cls, kwargs = current.handle_events(events)
        current.update(dt)
        current.draw()

        if next_cls is not None:
            current = next_cls(surface, state, **kwargs)

        steam.run_callbacks()
        pygame.display.flip()

    steam.shutdown()
    state.close()
    pygame.quit()
    sys.exit()


if __name__ == "__main__":
    main()
