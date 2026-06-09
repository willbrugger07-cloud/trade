"""
Core card-generation and pack-opening engine.
Operates on plain dataclasses so it can be called from both the CLI and the API.
"""
import random
import uuid
from dataclasses import dataclass, field
from typing import Optional

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------
TIER_PROBABILITIES: dict[str, float] = {
    "Base": 70.0,
    "Insert": 18.0,
    "Refractor": 8.0,
    "Autograph": 3.5,
    "Patch Auto": 0.49,
    "Immortal 1-of-1": 0.01,
}

TIER_MULTIPLIERS: dict[str, float] = {
    "Base": 1,
    "Insert": 3,
    "Refractor": 5,
    "Autograph": 15,
    "Patch Auto": 50,
    "Immortal 1-of-1": 500,
}

PLAYER_POOL: dict[str, list[str]] = {
    "Basketball": [
        "LeBron James", "Michael Jordan", "Steph Curry",
        "Giannis Antetokounmpo", "Luka Doncic",
    ],
    "Soccer": [
        "Lionel Messi", "Cristiano Ronaldo", "Kylian Mbappé",
        "Erling Haaland", "Neymar Jr",
    ],
    "Football": [
        "Patrick Mahomes", "Tom Brady", "Lamar Jackson",
        "Justin Jefferson", "Travis Kelce",
    ],
}

PACK_TYPES: dict[str, dict] = {
    "Standard":      {"cost": 10,  "cards": 4,  "boost": 1.0},
    "Hobby":         {"cost": 50,  "cards": 5,  "boost": 3.5},
    "Jumbo Premium": {"cost": 120, "cards": 10, "boost": 7.0},
}

BIG_HIT_TIERS = {"Autograph", "Patch Auto", "Immortal 1-of-1"}

# ---------------------------------------------------------------------------
# Dataclass (transport object — not a DB model)
# ---------------------------------------------------------------------------
@dataclass
class CardResult:
    card_id: str
    sport: str
    player: str
    tier: str
    base_rating: int
    market_value: float
    serial_number: Optional[str]
    is_big_hit: bool = field(init=False)

    def __post_init__(self):
        self.is_big_hit = self.tier in BIG_HIT_TIERS

    def display(self) -> str:
        serial = f" [{self.serial_number}]" if self.serial_number else ""
        return (
            f"{'✨' if self.is_big_hit else '  '} {self.tier.upper()}{serial}"
            f" — {self.player} ({self.sport})"
            f" | OVR: {self.base_rating} | Est. Value: ${self.market_value:.2f}"
        )


# ---------------------------------------------------------------------------
# Engine functions (pure / stateless)
# ---------------------------------------------------------------------------
def _weighted_tier(luck_boost: float) -> str:
    tiers = list(TIER_PROBABILITIES.keys())
    weights = []
    for tier in tiers:
        prob = TIER_PROBABILITIES[tier]
        if tier != "Base":
            prob *= luck_boost
        weights.append(prob)
    total = sum(weights)
    normalized = [w / total for w in weights]
    return random.choices(tiers, weights=normalized, k=1)[0]


def _serial_for_tier(tier: str) -> Optional[str]:
    if tier == "Immortal 1-of-1":
        return "1/1"
    if tier == "Patch Auto":
        return f"{random.randint(1, 10)}/10"
    if tier == "Autograph":
        return f"{random.randint(1, 99)}/99"
    return None


def generate_card(luck_boost: float = 1.0) -> CardResult:
    sport = random.choice(list(PLAYER_POOL.keys()))
    player = random.choice(PLAYER_POOL[sport])
    tier = _weighted_tier(luck_boost)
    base_rating = random.randint(75, 99)
    market_value = round(
        base_rating * TIER_MULTIPLIERS[tier] * random.uniform(0.8, 1.2), 2
    )
    return CardResult(
        card_id=str(uuid.uuid4())[:8],
        sport=sport,
        player=player,
        tier=tier,
        base_rating=base_rating,
        market_value=market_value,
        serial_number=_serial_for_tier(tier),
    )


def open_pack(pack_name: str) -> tuple[list[CardResult], float]:
    """
    Returns (cards_pulled, cost). Raises ValueError for unknown pack names.
    Does NOT touch balance — callers are responsible for that.
    """
    if pack_name not in PACK_TYPES:
        raise ValueError(f"Unknown pack type: {pack_name!r}")
    pack = PACK_TYPES[pack_name]
    cards = [generate_card(pack["boost"]) for _ in range(pack["cards"])]
    return cards, pack["cost"]
