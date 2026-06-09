"""
Shared game state — user session and database connection.
The SQLite file lives in the OS user-data directory so it survives
Steam updates and is never inside the read-only install folder.
"""
from pathlib import Path

import platformdirs
from sqlalchemy.orm import Session

from card_simulator.models import User, make_engine

APP_NAME = "SportsCardSimulator"


def _data_dir() -> Path:
    d = Path(platformdirs.user_data_dir(APP_NAME))
    d.mkdir(parents=True, exist_ok=True)
    return d


class GameState:
    def __init__(self):
        db_path = _data_dir() / "collection.db"
        self._engine, SessionFactory = make_engine(f"sqlite:///{db_path}")
        self.db: Session = SessionFactory()
        self.user: User | None = None

    def load_user(self, username: str) -> User:
        user = self.db.query(User).filter(User.username == username).first()
        if not user:
            user = User(username=username)
            self.db.add(user)
            self.db.commit()
            self.db.refresh(user)
        self.user = user
        return user

    def refresh_user(self):
        if self.user:
            self.db.refresh(self.user)

    def close(self):
        self.db.close()
        self._engine.dispose()
