"""
FastAPI backend for the Sports Card Simulator.

Run:  uvicorn card_simulator.api:app --reload

Endpoints:
  POST /users/{username}              — create or fetch user
  GET  /users/{username}              — wallet + collection summary
  POST /users/{username}/packs/{name} — open a pack
  GET  /users/{username}/cards        — full binder
  POST /users/{username}/cards/{id}/sell   — sell a card
  POST /users/{username}/cards/{id}/grade  — submit card for grading
"""
from datetime import datetime, timezone

from fastapi import Depends, FastAPI, HTTPException
from pydantic import BaseModel
from sqlalchemy.orm import Session

from .engine import PACK_TYPES, open_pack
from .grading import GRADING_FEE, grade_card
from .models import Card, SessionLocal, User, init_db

app = FastAPI(title="Sports Card Simulator API", version="2.0.0")

init_db()


# ---------------------------------------------------------------------------
# DB dependency
# ---------------------------------------------------------------------------
def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()


# ---------------------------------------------------------------------------
# Pydantic response schemas
# ---------------------------------------------------------------------------
class CardSchema(BaseModel):
    id: str
    sport: str
    player: str
    tier: str
    base_rating: int
    market_value: float
    serial_number: str | None
    grade: float | None
    graded_value: float | None
    graded_at: datetime | None
    pulled_at: datetime

    model_config = {"from_attributes": True}


class UserSummary(BaseModel):
    username: str
    balance: float
    card_count: int
    collection_value: float


class PackOpenResponse(BaseModel):
    balance_after: float
    cost: float
    cards: list[CardSchema]


class GradeResponse(BaseModel):
    centering: float
    corners: float
    edges: float
    surface: float
    final_grade: float
    multiplier: float
    graded_value: float
    fee: float
    balance_after: float
    card: CardSchema


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------
def _get_user(username: str, db: Session) -> User:
    user = db.query(User).filter(User.username == username).first()
    if not user:
        raise HTTPException(status_code=404, detail=f"User '{username}' not found.")
    return user


def _card_to_schema(card: Card) -> CardSchema:
    return CardSchema.model_validate(card)


# ---------------------------------------------------------------------------
# Routes
# ---------------------------------------------------------------------------
@app.post("/users/{username}", response_model=UserSummary, status_code=201)
def create_user(username: str, db: Session = Depends(get_db)):
    existing = db.query(User).filter(User.username == username).first()
    if existing:
        raise HTTPException(status_code=409, detail="Username already taken.")
    user = User(username=username)
    db.add(user)
    db.commit()
    db.refresh(user)
    return UserSummary(
        username=user.username,
        balance=user.balance,
        card_count=0,
        collection_value=0.0,
    )


@app.get("/users/{username}", response_model=UserSummary)
def get_user(username: str, db: Session = Depends(get_db)):
    user = _get_user(username, db)
    value = sum(
        (c.graded_value if c.graded_value is not None else c.market_value)
        for c in user.cards
    )
    return UserSummary(
        username=user.username,
        balance=user.balance,
        card_count=len(user.cards),
        collection_value=round(value, 2),
    )


@app.post("/users/{username}/packs/{pack_name}", response_model=PackOpenResponse)
def buy_pack(username: str, pack_name: str, db: Session = Depends(get_db)):
    user = _get_user(username, db)

    if pack_name not in PACK_TYPES:
        raise HTTPException(status_code=400, detail=f"Unknown pack: '{pack_name}'")

    cost = PACK_TYPES[pack_name]["cost"]
    if user.balance < cost:
        raise HTTPException(
            status_code=402,
            detail=f"Insufficient funds. Need ${cost}, have ${user.balance:.2f}.",
        )

    cards_pulled, cost = open_pack(pack_name)
    user.balance = round(user.balance - cost, 2)

    db_cards: list[Card] = []
    for cr in cards_pulled:
        db_card = Card(
            id=cr.card_id,
            owner_id=user.id,
            sport=cr.sport,
            player=cr.player,
            tier=cr.tier,
            base_rating=cr.base_rating,
            market_value=cr.market_value,
            serial_number=cr.serial_number,
        )
        db.add(db_card)
        db_cards.append(db_card)

    db.commit()
    for c in db_cards:
        db.refresh(c)

    return PackOpenResponse(
        balance_after=user.balance,
        cost=cost,
        cards=[_card_to_schema(c) for c in db_cards],
    )


@app.get("/users/{username}/cards", response_model=list[CardSchema])
def list_cards(username: str, db: Session = Depends(get_db)):
    user = _get_user(username, db)
    return [_card_to_schema(c) for c in user.cards]


@app.delete("/users/{username}/cards/{card_id}")
def sell_card(username: str, card_id: str, db: Session = Depends(get_db)):
    user = _get_user(username, db)
    card = db.query(Card).filter(Card.id == card_id, Card.owner_id == user.id).first()
    if not card:
        raise HTTPException(status_code=404, detail="Card not found in your collection.")

    sale_price = card.graded_value if card.graded_value is not None else card.market_value
    user.balance = round(user.balance + sale_price, 2)
    db.delete(card)
    db.commit()
    return {"sold": card_id, "sale_price": sale_price, "balance_after": user.balance}


@app.post("/users/{username}/cards/{card_id}/grade", response_model=GradeResponse)
def submit_for_grading(username: str, card_id: str, db: Session = Depends(get_db)):
    user = _get_user(username, db)

    if user.balance < GRADING_FEE:
        raise HTTPException(
            status_code=402,
            detail=f"Grading costs ${GRADING_FEE}. You only have ${user.balance:.2f}.",
        )

    card = db.query(Card).filter(Card.id == card_id, Card.owner_id == user.id).first()
    if not card:
        raise HTTPException(status_code=404, detail="Card not found in your collection.")
    if card.grade is not None:
        raise HTTPException(status_code=409, detail="Card has already been graded.")

    result = grade_card(card.market_value)

    user.balance = round(user.balance - GRADING_FEE, 2)
    card.grade = result.final_grade
    card.graded_value = result.graded_value
    card.graded_at = datetime.now(timezone.utc)
    db.commit()
    db.refresh(card)

    return GradeResponse(
        centering=result.centering,
        corners=result.corners,
        edges=result.edges,
        surface=result.surface,
        final_grade=result.final_grade,
        multiplier=result.multiplier,
        graded_value=result.graded_value,
        fee=GRADING_FEE,
        balance_after=user.balance,
        card=_card_to_schema(card),
    )
