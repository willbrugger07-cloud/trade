"""
Terminal front-end — wraps the same engine + DB the API uses.
Usage: python -m card_simulator.cli <username>
"""
import sys
import time

from .engine import PACK_TYPES, open_pack
from .grading import GRADING_FEE, grade_card
from .models import Card, SessionLocal, User, init_db

MENU = """
What would you like to do?
  1) Open Standard Pack      ($10)
  2) Open Hobby Pack         ($50)  [Increased hit rate]
  3) Open Jumbo Premium Pack ($120) [High quantity & best luck]
  4) View card binder & balance
  5) Sell a card
  6) Grade a card            ($25 fee)
  7) Exit
"""


def _get_or_create_user(db, username: str) -> User:
    user = db.query(User).filter(User.username == username).first()
    if not user:
        user = User(username=username)
        db.add(user)
        db.commit()
        db.refresh(user)
        print(f"🆕 New account created for '{username}'. Starting balance: $200.00")
    else:
        print(f"👋 Welcome back, {username}! Balance: ${user.balance:.2f}")
    return user


def cmd_open_pack(user: User, pack_name: str, db):
    pack_cfg = PACK_TYPES[pack_name]
    if user.balance < pack_cfg["cost"]:
        print(f"❌ Insufficient funds. Need ${pack_cfg['cost']}, have ${user.balance:.2f}.")
        return

    cards, cost = open_pack(pack_name)
    user.balance = round(user.balance - cost, 2)

    print(f"\n🎒 Opening {pack_name.upper()} Pack… (${cost:.0f} deducted)")
    print("⚡ Ripping wrap… " + "• " * len(cards))
    time.sleep(0.8)

    print("\n--- 📦 PACK REVEAL ---")
    for i, cr in enumerate(cards, 1):
        if cr.is_big_hit:
            print(f"🚨 Card {i}: 🔥🔥🔥 BIG HIT! 🔥🔥🔥")
        else:
            print(f"   Card {i}:")
        print(f"   > {cr.display()}")
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
        time.sleep(0.3)

    db.commit()
    print("----------------------")
    print(f"💰 Remaining balance: ${user.balance:.2f}")


def cmd_view_collection(user: User, db):
    db.refresh(user)
    print(f"\n=== 📁 YOUR BINDER (Total Cards: {len(user.cards)}) ===")
    if not user.cards:
        print("Your binder is empty. Buy a pack to get started!")
        return

    total_value = 0.0
    for card in user.cards:
        effective = card.graded_value if card.graded_value is not None else card.market_value
        grade_tag = f" PSA {card.grade:.1f}" if card.grade else ""
        serial = f" [{card.serial_number}]" if card.serial_number else ""
        print(
            f"  [{card.id}] {card.tier.upper()}{serial}{grade_tag}"
            f" — {card.player} ({card.sport})"
            f" | OVR: {card.base_rating} | Value: ${effective:.2f}"
        )
        total_value += effective

    print(f"\n💰 Wallet: ${user.balance:.2f}  |  📈 Collection value: ${total_value:.2f}")


def cmd_sell(user: User, card_id: str, db):
    card = db.query(Card).filter(Card.id == card_id, Card.owner_id == user.id).first()
    if not card:
        print("❌ Card ID not found in your collection.")
        return
    price = card.graded_value if card.graded_value is not None else card.market_value
    user.balance = round(user.balance + price, 2)
    db.delete(card)
    db.commit()
    print(f"🤝 Sold {card.player} ({card.tier}) for ${price:.2f}! Balance: ${user.balance:.2f}")


def cmd_grade(user: User, card_id: str, db):
    if user.balance < GRADING_FEE:
        print(f"❌ Grading costs ${GRADING_FEE}. You only have ${user.balance:.2f}.")
        return

    card = db.query(Card).filter(Card.id == card_id, Card.owner_id == user.id).first()
    if not card:
        print("❌ Card ID not found.")
        return
    if card.grade is not None:
        print(f"⚠️  This card is already graded PSA {card.grade:.1f}.")
        return

    print("🔬 Submitting card to the grading lab…")
    time.sleep(1.0)

    result = grade_card(card.market_value)
    user.balance = round(user.balance - GRADING_FEE, 2)
    card.grade = result.final_grade
    card.graded_value = result.graded_value
    card.graded_at = result.graded_at
    db.commit()

    print(result.summary())
    print(f"💰 Grading fee deducted. Remaining balance: ${user.balance:.2f}")


def main():
    username = sys.argv[1] if len(sys.argv) > 1 else input("Enter username: ").strip()
    init_db()
    db = SessionLocal()

    try:
        user = _get_or_create_user(db, username)

        while True:
            print(MENU, end="")
            choice = input("Enter choice (1-7): ").strip()

            if choice == "1":
                cmd_open_pack(user, "Standard", db)
            elif choice == "2":
                cmd_open_pack(user, "Hobby", db)
            elif choice == "3":
                cmd_open_pack(user, "Jumbo Premium", db)
            elif choice == "4":
                cmd_view_collection(user, db)
            elif choice == "5":
                cid = input("Card ID to sell: ").strip()
                cmd_sell(user, cid, db)
            elif choice == "6":
                cid = input("Card ID to grade: ").strip()
                cmd_grade(user, cid, db)
            elif choice == "7":
                print("Thanks for ripping packs! Goodbye.")
                break
            else:
                print("❌ Invalid selection.")
    finally:
        db.close()


if __name__ == "__main__":
    main()
