import os
from dotenv import load_dotenv
import robin_stocks.robinhood as rh

load_dotenv()


def login():
    username = os.getenv("ROBINHOOD_USERNAME")
    password = os.getenv("ROBINHOOD_PASSWORD")
    mfa_code = os.getenv("ROBINHOOD_MFA_CODE") or None

    if not username or not password:
        raise ValueError("Set ROBINHOOD_USERNAME and ROBINHOOD_PASSWORD in .env")

    rh.login(username, password, mfa_code=mfa_code)
    print("Logged in to Robinhood.")


def get_portfolio():
    profile = rh.load_portfolio_profile()
    return {
        "equity": profile.get("equity"),
        "extended_hours_equity": profile.get("extended_hours_equity"),
        "withdrawable_amount": profile.get("withdrawable_amount"),
    }


def get_positions():
    positions = rh.get_open_stock_positions()
    result = []
    for pos in positions:
        instrument_url = pos.get("instrument")
        symbol = rh.get_symbol_by_url(instrument_url)
        result.append({
            "symbol": symbol,
            "quantity": pos.get("quantity"),
            "average_buy_price": pos.get("average_buy_price"),
        })
    return result


def get_quote(symbol: str):
    quote = rh.get_latest_price(symbol)
    return {"symbol": symbol.upper(), "price": quote[0] if quote else None}


def place_buy_order(symbol: str, quantity: float):
    order = rh.order_buy_market(symbol, quantity)
    return order


def place_sell_order(symbol: str, quantity: float):
    order = rh.order_sell_market(symbol, quantity)
    return order


def logout():
    rh.logout()
    print("Logged out of Robinhood.")


if __name__ == "__main__":
    login()

    print("\n--- Portfolio ---")
    portfolio = get_portfolio()
    for k, v in portfolio.items():
        print(f"  {k}: {v}")

    print("\n--- Open Positions ---")
    positions = get_positions()
    if positions:
        for p in positions:
            print(f"  {p['symbol']}: {p['quantity']} shares @ avg ${p['average_buy_price']}")
    else:
        print("  No open positions.")

    logout()
