"""
Momentum trading bot for SPY.

Strategy:
  - At market open, record the opening price.
  - If price moves up >= ENTRY_MOMENTUM_PCT from open, buy $TRADE_AMOUNT worth of SPY.
  - After entry, exit if:
      * Price rises >= TAKE_PROFIT_PCT from entry  (take profit)
      * Price falls <= STOP_LOSS_PCT from entry    (stop loss)
  - Only one position open at a time per session.

Configure via .env or environment variables.
"""

import os
import time
import logging
from datetime import datetime, time as dt_time
import robin_stocks.robinhood as rh
from dotenv import load_dotenv

load_dotenv()

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s  %(levelname)s  %(message)s",
    datefmt="%Y-%m-%d %H:%M:%S",
)
log = logging.getLogger(__name__)

# --- Config (override via .env) ---
SYMBOL             = os.getenv("SYMBOL", "SPY")
TRADE_AMOUNT_USD   = float(os.getenv("TRADE_AMOUNT_USD", "20"))    # dollars per trade
ENTRY_MOMENTUM_PCT = float(os.getenv("ENTRY_MOMENTUM_PCT", "0.3")) # % move from open to enter
TAKE_PROFIT_PCT    = float(os.getenv("TAKE_PROFIT_PCT", "0.5"))    # % gain to exit with profit
STOP_LOSS_PCT      = float(os.getenv("STOP_LOSS_PCT", "0.3"))      # % loss to cut position
POLL_SECONDS       = int(os.getenv("POLL_SECONDS", "30"))          # how often to check price

MARKET_OPEN  = dt_time(9, 30)
MARKET_CLOSE = dt_time(16, 0)


def current_price(symbol: str) -> float:
    prices = rh.get_latest_price(symbol)
    return float(prices[0])


def shares_for_amount(price: float, amount_usd: float) -> float:
    """Fractional shares rounded to 6 decimal places."""
    return round(amount_usd / price, 6)


def market_is_open() -> bool:
    now = datetime.now().time()
    return MARKET_OPEN <= now <= MARKET_CLOSE


def run():
    username = os.getenv("ROBINHOOD_USERNAME")
    password = os.getenv("ROBINHOOD_PASSWORD")
    mfa_code = os.getenv("ROBINHOOD_MFA_CODE") or None

    if not username or not password:
        raise ValueError("Set ROBINHOOD_USERNAME and ROBINHOOD_PASSWORD in .env")

    rh.login(username, password, mfa_code=mfa_code)
    log.info("Logged in to Robinhood.")

    open_price: float | None = None
    entry_price: float | None = None
    shares_held: float = 0.0
    traded_today = False

    try:
        while True:
            if not market_is_open():
                if datetime.now().time() > MARKET_CLOSE:
                    log.info("Market closed. Exiting.")
                    break
                log.info("Waiting for market open…")
                time.sleep(30)
                continue

            price = current_price(SYMBOL)
            log.info(f"{SYMBOL} ${price:.2f}")

            # Capture opening price on first tick
            if open_price is None:
                open_price = price
                log.info(f"Opening price set: ${open_price:.2f}")

            # --- Entry logic ---
            if entry_price is None and not traded_today:
                pct_from_open = (price - open_price) / open_price * 100
                log.info(f"  {pct_from_open:+.2f}% from open (need +{ENTRY_MOMENTUM_PCT}% to enter)")
                if pct_from_open >= ENTRY_MOMENTUM_PCT:
                    qty = shares_for_amount(price, TRADE_AMOUNT_USD)
                    log.info(f"  MOMENTUM TRIGGER — buying {qty} shares @ ~${price:.2f}")
                    order = rh.order_buy_fractional_by_quantity(SYMBOL, qty)
                    if order and order.get("id"):
                        entry_price = price
                        shares_held = qty
                        log.info(f"  BUY order placed: {order['id']}")
                    else:
                        log.warning(f"  Buy order failed or rejected: {order}")

            # --- Exit logic ---
            elif entry_price is not None and shares_held > 0:
                pct_from_entry = (price - entry_price) / entry_price * 100
                log.info(f"  {pct_from_entry:+.2f}% from entry "
                         f"(TP +{TAKE_PROFIT_PCT}% / SL -{STOP_LOSS_PCT}%)")

                should_exit = False
                reason = ""
                if pct_from_entry >= TAKE_PROFIT_PCT:
                    should_exit = True
                    reason = "TAKE PROFIT"
                elif pct_from_entry <= -STOP_LOSS_PCT:
                    should_exit = True
                    reason = "STOP LOSS"

                if should_exit:
                    log.info(f"  {reason} — selling {shares_held} shares @ ~${price:.2f}")
                    order = rh.order_sell_fractional_by_quantity(SYMBOL, shares_held)
                    if order and order.get("id"):
                        pnl = (price - entry_price) * shares_held
                        log.info(f"  SELL order placed: {order['id']}  est. P&L: ${pnl:+.2f}")
                    else:
                        log.warning(f"  Sell order failed or rejected: {order}")
                    entry_price = None
                    shares_held = 0.0
                    traded_today = True  # one trade per session

            time.sleep(POLL_SECONDS)

    except KeyboardInterrupt:
        log.info("Interrupted by user.")

        # Emergency exit: sell any open position
        if shares_held > 0:
            log.warning(f"Emergency exit: selling {shares_held} shares of {SYMBOL}.")
            rh.order_sell_fractional_by_quantity(SYMBOL, shares_held)

    finally:
        rh.logout()
        log.info("Logged out.")


if __name__ == "__main__":
    run()
