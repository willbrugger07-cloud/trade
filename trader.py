"""
Aggressive momentum trading bot.

Strategy:
  - Watches multiple tickers simultaneously.
  - Enters when any ticker moves +ENTRY_MOMENTUM_PCT from its session open.
  - Exits at TAKE_PROFIT_PCT gain (let winners run) or STOP_LOSS_PCT loss (cut fast).
  - No limit on number of trades per day.
  - Only one open position at a time (protects the $20 balance).
"""

import os
import time
import logging
from datetime import datetime, time as dt_time
from zoneinfo import ZoneInfo
import robin_stocks.robinhood as rh
from dotenv import load_dotenv

ET = ZoneInfo("America/New_York")

load_dotenv()

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s  %(levelname)s  %(message)s",
    datefmt="%Y-%m-%d %H:%M:%S",
)
log = logging.getLogger(__name__)

# --- Config ---
TICKERS            = os.getenv("TICKERS", "SPY,QQQ,AAPL,TSLA,NVDA").split(",")
TRADE_AMOUNT_USD   = float(os.getenv("TRADE_AMOUNT_USD", "20"))
ENTRY_MOMENTUM_PCT = float(os.getenv("ENTRY_MOMENTUM_PCT", "0.15"))
TAKE_PROFIT_PCT    = float(os.getenv("TAKE_PROFIT_PCT", "0.8"))   # let winners run
STOP_LOSS_PCT      = float(os.getenv("STOP_LOSS_PCT", "0.2"))     # cut losses fast
POLL_SECONDS       = int(os.getenv("POLL_SECONDS", "20"))         # faster polling

MARKET_OPEN  = dt_time(9, 30)
MARKET_CLOSE = dt_time(15, 50)   # stop entering new trades 10 min before close


def get_prices(symbols: list[str]) -> dict[str, float]:
    prices = rh.get_latest_price(symbols)
    return {sym: float(p) for sym, p in zip(symbols, prices) if p}


def shares_for_amount(price: float, amount_usd: float) -> float:
    return round(amount_usd / price, 6)


def market_is_open() -> bool:
    now = datetime.now(ET).time()
    return MARKET_OPEN <= now <= MARKET_CLOSE


def run():
    username = os.getenv("ROBINHOOD_USERNAME")
    password = os.getenv("ROBINHOOD_PASSWORD")
    mfa_code = os.getenv("ROBINHOOD_MFA_CODE") or None

    if not username or not password:
        raise ValueError("Set ROBINHOOD_USERNAME and ROBINHOOD_PASSWORD in .env")

    rh.login(username, password, mfa_code=mfa_code)
    log.info(f"Logged in. Watching: {', '.join(TICKERS)}")

    open_prices: dict[str, float] = {}
    entry_symbol: str | None = None
    entry_price: float = 0.0
    shares_held: float = 0.0
    total_pnl: float = 0.0
    trade_count: int = 0

    try:
        while True:
            if not market_is_open():
                if datetime.now(ET).time() > MARKET_CLOSE:
                    log.info(f"Market closed. Trades today: {trade_count}  Total P&L: ${total_pnl:+.2f}")
                    break
                log.info("Waiting for market open…")
                time.sleep(30)
                continue

            prices = get_prices(TICKERS)

            # Capture opening prices
            for sym, price in prices.items():
                if sym not in open_prices:
                    open_prices[sym] = price
                    log.info(f"  Open price {sym}: ${price:.2f}")

            # --- Exit logic (priority over entry) ---
            if entry_symbol and shares_held > 0:
                price = prices.get(entry_symbol)
                if price:
                    pct = (price - entry_price) / entry_price * 100
                    log.info(f"[IN {entry_symbol}] ${price:.2f}  {pct:+.2f}%  "
                             f"(TP +{TAKE_PROFIT_PCT}% / SL -{STOP_LOSS_PCT}%)")

                    if pct >= TAKE_PROFIT_PCT:
                        _exit(entry_symbol, shares_held, price, entry_price, "TAKE PROFIT")
                        total_pnl += (price - entry_price) * shares_held
                        trade_count += 1
                        entry_symbol, shares_held, entry_price = None, 0.0, 0.0
                    elif pct <= -STOP_LOSS_PCT:
                        _exit(entry_symbol, shares_held, price, entry_price, "STOP LOSS")
                        total_pnl += (price - entry_price) * shares_held
                        trade_count += 1
                        entry_symbol, shares_held, entry_price = None, 0.0, 0.0

            # --- Entry logic (only when flat) ---
            if not entry_symbol:
                best_sym = None
                best_pct = ENTRY_MOMENTUM_PCT  # must beat threshold

                for sym, price in prices.items():
                    if sym not in open_prices:
                        continue
                    pct = (price - open_prices[sym]) / open_prices[sym] * 100
                    log.info(f"  {sym} ${price:.2f}  {pct:+.2f}% from open")
                    if pct > best_pct:
                        best_pct = pct
                        best_sym = sym

                if best_sym:
                    price = prices[best_sym]
                    qty = shares_for_amount(price, TRADE_AMOUNT_USD)
                    log.info(f"ENTRY: {best_sym} +{best_pct:.2f}% — buying {qty} shares @ ~${price:.2f}")
                    order = rh.order_buy_fractional_by_quantity(best_sym, qty)
                    if order and order.get("id"):
                        entry_symbol = best_sym
                        entry_price = price
                        shares_held = qty
                        log.info(f"  BUY order placed: {order['id']}")
                    else:
                        log.warning(f"  Buy order failed: {order}")

            time.sleep(POLL_SECONDS)

    except KeyboardInterrupt:
        log.info("Interrupted.")
        if entry_symbol and shares_held > 0:
            price = get_prices([entry_symbol]).get(entry_symbol, entry_price)
            log.warning(f"Emergency exit: selling {shares_held} {entry_symbol}")
            rh.order_sell_fractional_by_quantity(entry_symbol, shares_held)
            total_pnl += (price - entry_price) * shares_held

    finally:
        log.info(f"Session summary — Trades: {trade_count}  P&L: ${total_pnl:+.2f}")
        rh.logout()
        log.info("Logged out.")


def _exit(symbol, qty, price, entry, reason):
    pnl = (price - entry) * qty
    log.info(f"{reason}: selling {qty} {symbol} @ ~${price:.2f}  est. P&L: ${pnl:+.2f}")
    order = rh.order_sell_fractional_by_quantity(symbol, qty)
    if order and order.get("id"):
        log.info(f"  SELL order placed: {order['id']}")
    else:
        log.warning(f"  Sell order failed: {order}")


if __name__ == "__main__":
    run()
