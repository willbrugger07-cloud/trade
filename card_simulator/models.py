"""
SQLAlchemy models — every user's collection and wallet persist across sessions.
"""
from datetime import datetime, timezone

from sqlalchemy import (
    Column, DateTime, Float, ForeignKey, Integer, String, create_engine
)
from sqlalchemy.orm import DeclarativeBase, relationship, sessionmaker

DATABASE_URL = "sqlite:///./card_simulator.db"

engine = create_engine(DATABASE_URL, connect_args={"check_same_thread": False})
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)


class Base(DeclarativeBase):
    pass


class User(Base):
    __tablename__ = "users"

    id = Column(Integer, primary_key=True, index=True)
    username = Column(String, unique=True, index=True, nullable=False)
    balance = Column(Float, default=200.0)
    created_at = Column(DateTime, default=lambda: datetime.now(timezone.utc))

    cards = relationship("Card", back_populates="owner", cascade="all, delete-orphan")


class Card(Base):
    __tablename__ = "cards"

    id = Column(String, primary_key=True)          # 8-char UUID hex
    owner_id = Column(Integer, ForeignKey("users.id"), nullable=False)
    sport = Column(String, nullable=False)
    player = Column(String, nullable=False)
    tier = Column(String, nullable=False)
    base_rating = Column(Integer, nullable=False)
    market_value = Column(Float, nullable=False)
    serial_number = Column(String, nullable=True)

    # Grading fields — null until graded
    grade = Column(Float, nullable=True)
    graded_value = Column(Float, nullable=True)
    graded_at = Column(DateTime, nullable=True)

    pulled_at = Column(DateTime, default=lambda: datetime.now(timezone.utc))

    owner = relationship("User", back_populates="cards")


def init_db():
    Base.metadata.create_all(bind=engine)
