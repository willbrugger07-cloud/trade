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
import uuid
from datetime import datetime, time as dt_time
from zoneinfo import ZoneInfo
import robin_stocks.robinhood as rh
import robin_stocks.robinhood.helper as rh_helper
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
STOP_LOSS_PCT      = float(os.getenv("STOP_LOSS_PCT", "0.5"))
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


def place_order(symbol: str, dollar_amount: float, side: str) -> dict:
    """Place a fractional dollar-based market order using robin_stocks built-ins."""
    if side == "buy":
        result = rh.order_buy_fractional_by_price(
            symbol, dollar_amount, timeInForce="gfd", extendedHours=False
        )
    else:
        result = rh.order_sell_fractional_by_price(
            symbol, dollar_amount, timeInForce="gfd", extendedHours=False
        )
    return result or {}


def market_is_open() -> bool:
    now = datetime.now(ET).time()
    return MARKET_OPEN <= now <= MARKET_CLOSE


def run():
    token = os.getenv("RH_TOKEN") or "eyJhbGciOiJFUzI1NiIsImtpZCI6IjIiLCJ0eXAiOiJKV1QifQ.eyJkY3QiOjE3ODEwMDI3MTksImRldmljZV9oYXNoIjoiOGM1Y2UxYmY2NDRjYmE1OGE3NGUyYjg2ZDMyMGI1NDYiLCJleHAiOjE3ODIzMzk1ODcsImlzcyI6Imh0dHBzOi8vYXBpLnJvYmluaG9vZC5jb20iLCJsZXZlbDJfYWNjZXNzIjp0cnVlLCJtZXRhIjp7Im9pZCI6ImM4MlNIMFdaT3NhYk9YR1Ayc3hxY2ozNEZ4a3ZmbldSWkJLbEJqRlMiLCJvbiI6IlJvYmluaG9vZCJ9LCJucyI6IlJIIiwib3B0aW9ucyI6dHJ1ZSwicG9zIjoicCIsInNjb3BlIjoiaW50ZXJuYWwiLCJzZXJ2aWNlX3JlY29yZHMiOlt7ImhhbHRlZCI6ZmFsc2UsInNlcnZpY2UiOiJudW1tdXNfdXMiLCJzaGFyZF9pZCI6Miwic3RhdGUiOiJhdmFpbGFibGUifSx7ImhhbHRlZCI6ZmFsc2UsInNlcnZpY2UiOiJjZXJlc191cyIsInNoYXJkX2lkIjoyLCJzdGF0ZSI6ImF2YWlsYWJsZSJ9LHsiaGFsdGVkIjpmYWxzZSwic2VydmljZSI6ImJyb2tlYmFja191cyIsInNoYXJkX2lkIjoxNywic3RhdGUiOiJhdmFpbGFibGUifV0sInNsZyI6MSwic2xzIjoiSmY2QkpEZ2Nyd2JMNjdLeHlyNThhMDJiV0FYSGVJYmU5ZXJ4VHVmM2RYK0dnaEpmVm9NQnF6OWo0a21EdGZzSXZacVJUeFFVRmNwcTFUUjkxTmNPRHc9PSIsInNybSI6eyJiIjp7ImhsIjpmYWxzZSwiciI6InVzIiwic2lkIjoxN30sImMiOnsiaGwiOmZhbHNlLCJyIjoidXMiLCJzaWQiOjJ9LCJuIjp7ImhsIjpmYWxzZSwiciI6InVzIiwic2lkIjoyfX0sInRva2VuIjoid0RPZ2dQR1V6ZFI5WVJBeU5JSVRDUVBGSlhLTUJzIiwidXNlcl9pZCI6IjNiNjg3YzE2LWE0YmYtNGFhMi05ZDY1LTUzZDQ3YzM4N2NlZiIsInVzZXJfb3JpZ2luIjoiVVMifQ.HKOP_Gx0Ayf-a-_WN0snPc84oXlI3da_TLR5xmIDZih3ZVia38NyGNeP3XjYqWzRDsRfwB8JgJKpaTHp0G47HQ"
    rh_helper.update_session("Authorization", "Bearer " + token)
    rh_helper.set_login_state(True)
    log.info("Auth token loaded. Starting bot...")
    log.info(f"Watching: {', '.join(TICKERS)}")

    # positions: {symbol: {"entry": price, "qty": shares, "dollars": amount}}
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
                log.info("Waiting for market open...")
                time.sleep(30)
                continue

            prices = get_prices(TICKERS)

            # Fallback: capture price for any ticker not in open_prices
            for sym, price in prices.items():
                if sym not in open_prices:
                    open_prices[sym] = price
                    log.info(f"  Fallback open {sym}: ${price:.2f}")

            # Force-exit all positions 5 minutes before close
            near_close = datetime.now(ET).time() >= dt_time(15, 45)

            # --- Exit logic ---
            for sym in list(positions.keys()):
                price = prices.get(sym)
                if not price:
                    continue
                pos = positions[sym]
                pct = (price - pos["entry"]) / pos["entry"] * 100
                log.info(f"[{sym}] ${price:.2f}  {pct:+.2f}%  (TP +{TAKE_PROFIT_PCT}% / SL -{STOP_LOSS_PCT}%)")

                if near_close:
                    _exit(sym, pos["dollars"], price, pos["entry"], "EOD CLOSE")
                    total_pnl += (price - pos["entry"]) * pos["qty"]
                    trade_count += 1
                    del positions[sym]
                    continue

                if pct >= TAKE_PROFIT_PCT:
                    _exit(sym, pos["dollars"], price, pos["entry"], "TAKE PROFIT")
                    total_pnl += (price - pos["entry"]) * pos["qty"]
                    trade_count += 1
                    del positions[sym]
                elif pct <= -STOP_LOSS_PCT:
                    _exit(sym, pos["dollars"], price, pos["entry"], "STOP LOSS")
                    total_pnl += (price - pos["entry"]) * pos["qty"]
                    trade_count += 1
                    del positions[sym]

            # --- Entry logic (fill open slots) ---
            slots_available = MAX_POSITIONS - len(positions)
            candidates = []
            if slots_available > 0:
                for sym, price in prices.items():
                    if sym in positions or sym not in open_prices:
                        continue
                    pct = (price - open_prices[sym]) / open_prices[sym] * 100
                    if pct >= ENTRY_MOMENTUM_PCT:
                        candidates.append((pct, sym, price))

                candidates.sort(reverse=True)

                for pct, sym, price in candidates[:slots_available]:
                    log.info(f"ENTRY: {sym} +{pct:.2f}% — buying ${per_trade} @ ~${price:.2f}")
                    try:
                        order = place_order(sym, per_trade, "buy")
                        order_id = order.get("id") if order else None
                        if order_id:
                            qty = shares_for_amount(price, per_trade)
                            positions[sym] = {"entry": price, "qty": qty, "dollars": per_trade}
                            log.info(f"  BUY order placed: {order_id}")
                        else:
                            log.warning(f"  Buy order failed: {order}")
                    except Exception as e:
                        log.warning(f"  Buy order error: {e}")

            if not candidates:
                for sym, price in prices.items():
                    if sym not in positions and sym in open_prices:
                        pct = (price - open_prices[sym]) / open_prices[sym] * 100
                        log.info(f"  {sym} ${price:.2f}  {pct:+.2f}%")

            time.sleep(POLL_SECONDS)

    except KeyboardInterrupt:
        log.info("Interrupted — emergency exit all positions.")
        for sym, pos in list(positions.items()):
            price = prices.get(sym, pos["entry"])
            log.warning(f"Emergency exit: selling ${pos['dollars']} of {sym}")
            try:
                place_order(sym, pos["dollars"], "sell")
            except Exception as e:
                log.warning(f"  Emergency sell error: {e}")
            total_pnl += (price - pos["entry"]) * pos["qty"]

    finally:
        log.info(f"Session summary — Trades: {trade_count}  P&L: ${total_pnl:+.2f}")
        log.info("Done.")


def _exit(symbol, dollar_amount, price, entry, reason):
    pnl_est = (price - entry) / entry * dollar_amount
    log.info(f"{reason}: selling ${dollar_amount} of {symbol} @ ~${price:.2f}  est. P&L: ${pnl_est:+.2f}")
    try:
        order = place_order(symbol, dollar_amount, "sell")
        if order and order.get("id"):
            log.info(f"  SELL order placed: {order['id']}")
        else:
            log.warning(f"  Sell order failed: {order}")
    except Exception as e:
        log.warning(f"  Sell order error: {e}")


if __name__ == "__main__":
    run()
