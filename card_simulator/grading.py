"""
PSA/BGS-style grading system.

Submitting a card costs a flat $25 fee.
The card is inspected across four sub-grades (centering, corners, edges,
surface) each scored 1–10. The final grade is the mean, rounded to the
nearest 0.5. A perfect 10 multiplies the card's market value by 10.
"""
import random
from dataclasses import dataclass
from datetime import datetime, timezone

GRADING_FEE = 25.0

# Grade → value multiplier
GRADE_MULTIPLIERS: dict[float, float] = {
    10.0: 10.0,
    9.5:  5.0,
    9.0:  3.0,
    8.5:  2.0,
    8.0:  1.5,
    7.5:  1.2,
    7.0:  1.0,
    # Anything below 7 is a haircut
    6.5:  0.85,
    6.0:  0.70,
    5.0:  0.55,
    4.0:  0.40,
    3.0:  0.30,
    2.0:  0.20,
    1.0:  0.10,
}


@dataclass
class GradingResult:
    centering: float
    corners: float
    edges: float
    surface: float
    final_grade: float
    multiplier: float
    graded_value: float
    fee: float = GRADING_FEE
    graded_at: datetime = None

    def __post_init__(self):
        if self.graded_at is None:
            self.graded_at = datetime.now(timezone.utc)

    def summary(self) -> str:
        return (
            f"📋 Grading Report\n"
            f"  Centering : {self.centering:.1f}\n"
            f"  Corners   : {self.corners:.1f}\n"
            f"  Edges     : {self.edges:.1f}\n"
            f"  Surface   : {self.surface:.1f}\n"
            f"  ─────────────────\n"
            f"  FINAL PSA : {self.final_grade:.1f} (×{self.multiplier})\n"
            f"  New Value : ${self.graded_value:.2f}"
        )


def _sub_grade() -> float:
    """Skew toward 7–9 to mimic realistic grading distributions."""
    return round(random.triangular(1, 10, 8.5) * 2) / 2


def _nearest_grade_key(raw: float) -> float:
    return min(GRADE_MULTIPLIERS.keys(), key=lambda k: abs(k - raw))


def grade_card(current_market_value: float) -> GradingResult:
    centering = _sub_grade()
    corners = _sub_grade()
    edges = _sub_grade()
    surface = _sub_grade()

    raw_mean = (centering + corners + edges + surface) / 4
    final_grade = round(raw_mean * 2) / 2   # round to nearest 0.5
    final_grade = max(1.0, min(10.0, final_grade))

    key = _nearest_grade_key(final_grade)
    multiplier = GRADE_MULTIPLIERS[key]
    graded_value = round(current_market_value * multiplier, 2)

    return GradingResult(
        centering=centering,
        corners=corners,
        edges=edges,
        surface=surface,
        final_grade=final_grade,
        multiplier=multiplier,
        graded_value=graded_value,
    )
