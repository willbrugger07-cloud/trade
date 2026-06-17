"""
Aggressive momentum trading bot — multi-position mode.

Strategy:
  - Watches 15 tickers simultaneously.
  - Splits available capital across up to 3 simultaneous positions.
  - Enters the top momentum movers (not already held).
  - Exits each position independently at TAKE_PROFIT_PCT or STOP_LOSS_PCT.
  - Re-enters immediately after each exit all day long.
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
TICKERS            = os.getenv("TICKERS", "SPY,QQQ,AAPL,TSLA,NVDA,MSFT,AMZN,META,AMD,GOOGL,NFLX,PLTR,SOFI,RIVN,COIN").split(",")
TOTAL_CAPITAL      = float(os.getenv("TRADE_AMOUNT_USD", "45"))
MAX_POSITIONS      = int(os.getenv("MAX_POSITIONS", "3"))
ENTRY_MOMENTUM_PCT = float(os.getenv("ENTRY_MOMENTUM_PCT", "0.15"))
TAKE_PROFIT_PCT    = float(os.getenv("TAKE_PROFIT_PCT", "0.8"))
STOP_LOSS_PCT      = float(os.getenv("STOP_LOSS_PCT", "0.2"))
POLL_SECONDS       = int(os.getenv("POLL_SECONDS", "20"))

MARKET_OPEN  = dt_time(9, 30)
MARKET_CLOSE = dt_time(15, 50)


def get_prices(symbols: list[str]) -> dict[str, float]:
    prices = rh.get_latest_price(symbols)
    return {sym: float(p) for sym, p in zip(symbols, prices) if p}


def get_open_prices(symbols: list[str]) -> dict[str, float]:
    result = {}
    for sym in symbols:
        try:
            historicals = rh.get_stock_historicals(sym, interval="5minute", span="day")
            if historicals:
                result[sym] = float(historicals[0]["open_price"])
        except Exception:
            pass
    return result


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

    rh.login(username, password, mfa_code=mfa_code, store_session=True, pickle_name="robinhood")
    log.info(f"Logged in. Watching: {', '.join(TICKERS)}")

    # positions: {symbol: {"entry": price, "qty": shares}}
    positions: dict[str, dict] = {}
    total_pnl: float = 0.0
    trade_count: int = 0

    # Split capital evenly across max positions
    per_trade = round(TOTAL_CAPITAL / MAX_POSITIONS, 2)
    log.info(f"Capital: ${TOTAL_CAPITAL} split into {MAX_POSITIONS} slots of ${per_trade} each")

    # Fetch real market open prices from historical data
    log.info("Fetching real open prices...")
    open_prices = get_open_prices(TICKERS)
    for sym, p in open_prices.items():
        log.info(f"  Real open {sym}: ${p:.2f}")

    try:
        while True:
            if not market_is_open():
                if datetime.now(ET).time() > MARKET_CLOSE:
                    log.info(f"Market closed. Trades: {trade_count}  Total P&L: ${total_pnl:+.2f}")
                    break
                log.info("Waiting for market open…")
                time.sleep(30)
                continue

            prices = get_prices(TICKERS)

            # Fallback: capture price for any ticker not in open_prices
            for sym, price in prices.items():
                if sym not in open_prices:
                    open_prices[sym] = price
                    log.info(f"  Fallback open {sym}: ${price:.2f}")

            # --- Exit logic ---
            for sym in list(positions.keys()):
                price = prices.get(sym)
                if not price:
                    continue
                pos = positions[sym]
                pct = (price - pos["entry"]) / pos["entry"] * 100
                log.info(f"[{sym}] ${price:.2f}  {pct:+.2f}%  (TP +{TAKE_PROFIT_PCT}% / SL -{STOP_LOSS_PCT}%)")

                if pct >= TAKE_PROFIT_PCT:
                    _exit(sym, pos["qty"], price, pos["entry"], "TAKE PROFIT")
                    total_pnl += (price - pos["entry"]) * pos["qty"]
                    trade_count += 1
                    del positions[sym]
                elif pct <= -STOP_LOSS_PCT:
                    _exit(sym, pos["qty"], price, pos["entry"], "STOP LOSS")
                    total_pnl += (price - pos["entry"]) * pos["qty"]
                    trade_count += 1
                    del positions[sym]

            # --- Entry logic (fill open slots) ---
            slots_available = MAX_POSITIONS - len(positions)
            if slots_available > 0:
                # Rank tickers by momentum, skip ones already held
                candidates = []
                for sym, price in prices.items():
                    if sym in positions or sym not in open_prices:
                        continue
                    pct = (price - open_prices[sym]) / open_prices[sym] * 100
                    if pct >= ENTRY_MOMENTUM_PCT:
                        candidates.append((pct, sym, price))

                candidates.sort(reverse=True)

                for pct, sym, price in candidates[:slots_available]:
                    qty = shares_for_amount(price, per_trade)
                    log.info(f"ENTRY: {sym} +{pct:.2f}% — buying {qty} shares @ ~${price:.2f}")
                    try:
                        qty = shares_for_amount(price, per_trade)
                        order = rh.order_buy_market(sym, qty, timeInForce="gfd")
                        if order and order.get("id"):
                            positions[sym] = {"entry": price, "qty": qty}
                            log.info(f"  BUY order placed: {order['id']}")
                        else:
                            log.warning(f"  Buy order failed: {order}")
                    except Exception as e:
                        log.warning(f"  Buy order error: {e}")

            if not candidates if slots_available > 0 else True:
                for sym, price in prices.items():
                    if sym not in positions and sym in open_prices:
                        pct = (price - open_prices[sym]) / open_prices[sym] * 100
                        log.info(f"  {sym} ${price:.2f}  {pct:+.2f}%")

            time.sleep(POLL_SECONDS)

    except KeyboardInterrupt:
        log.info("Interrupted — emergency exit all positions.")
        for sym, pos in list(positions.items()):
            price = prices.get(sym, pos["entry"])
            log.warning(f"Emergency exit: selling {pos['qty']} {sym}")
            rh.order_sell_fractional_by_quantity(sym, pos["qty"])
            total_pnl += (price - pos["entry"]) * pos["qty"]

    finally:
        log.info(f"Session summary — Trades: {trade_count}  P&L: ${total_pnl:+.2f}")
        rh.logout()
        log.info("Logged out.")


def _exit(symbol, qty, price, entry, reason):
    pnl = (price - entry) * qty
    log.info(f"{reason}: selling {qty} {symbol} @ ~${price:.2f}  est. P&L: ${pnl:+.2f}")
    try:
        order = rh.order_sell_market(symbol, round(qty, 6), timeInForce="gfd")
        if order and order.get("id"):
            log.info(f"  SELL order placed: {order['id']}")
        else:
            log.warning(f"  Sell order failed: {order}")
    except Exception as e:
        log.warning(f"  Sell order error: {e}")


if __name__ == "__main__":
    run()
