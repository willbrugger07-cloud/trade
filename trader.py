"""
Aggressive momentum trading bot — max profit mode.

Strategy:
  - Watches 15 tickers simultaneously.
  - Goes all-in on the single strongest mover (1 position = full capital).
  - Trailing stop: once up 0.5%, stop follows price to lock in gains.
  - Take profit at 2.0%, stop loss at 0.5%.
  - Re-enters immediately after each exit all day long.
  - Force-exits everything at 3:45 PM ET to avoid overnight risk.
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
ENTRY_MOMENTUM_PCT = float(os.getenv("ENTRY_MOMENTUM_PCT", "0.3"))
TAKE_PROFIT_PCT    = float(os.getenv("TAKE_PROFIT_PCT", "2.0"))
STOP_LOSS_PCT      = float(os.getenv("STOP_LOSS_PCT", "0.5"))
TRAIL_ACTIVATE_PCT = float(os.getenv("TRAIL_ACTIVATE_PCT", "0.5"))  # start trailing after +0.5%
TRAIL_DISTANCE_PCT = float(os.getenv("TRAIL_DISTANCE_PCT", "0.4"))  # trail 0.4% below peak
POLL_SECONDS       = int(os.getenv("POLL_SECONDS", "15"))

MARKET_OPEN  = dt_time(9, 30)
MARKET_CLOSE = dt_time(15, 50)
EOD_EXIT     = dt_time(15, 45)


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
    log.info(f"Strategy: all-in on top mover | TP +{TAKE_PROFIT_PCT}% | SL -{STOP_LOSS_PCT}% | Trail after +{TRAIL_ACTIVATE_PCT}%")
    log.info(f"Watching: {', '.join(TICKERS)}")

    # Single position at a time for max capital concentration
    # position: {"entry", "qty", "dollars", "peak"}
    position: dict | None = None
    position_sym: str | None = None
    total_pnl: float = 0.0
    trade_count: int = 0

    log.info("Fetching open prices...")
    open_prices = get_open_prices(TICKERS)
    for sym, p in open_prices.items():
        log.info(f"  {sym}: ${p:.2f}")

    prices: dict[str, float] = {}

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

            for sym, price in prices.items():
                if sym not in open_prices:
                    open_prices[sym] = price

            near_close = datetime.now(ET).time() >= EOD_EXIT

            # --- Exit logic ---
            if position and position_sym:
                price = prices.get(position_sym)
                if price:
                    pct = (price - position["entry"]) / position["entry"] * 100

                    # Update trailing stop peak
                    if price > position["peak"]:
                        position["peak"] = price

                    peak_pct = (position["peak"] - position["entry"]) / position["entry"] * 100
                    trail_stop_pct = peak_pct - TRAIL_DISTANCE_PCT if peak_pct >= TRAIL_ACTIVATE_PCT else None
                    trail_active = trail_stop_pct is not None

                    if trail_active:
                        log.info(f"[{position_sym}] ${price:.2f}  {pct:+.2f}%  peak={peak_pct:+.2f}%  trail_stop={trail_stop_pct:+.2f}%")
                    else:
                        log.info(f"[{position_sym}] ${price:.2f}  {pct:+.2f}%  (TP +{TAKE_PROFIT_PCT}% / SL -{STOP_LOSS_PCT}%)")

                    reason = None
                    if near_close:
                        reason = "EOD CLOSE"
                    elif pct >= TAKE_PROFIT_PCT:
                        reason = "TAKE PROFIT"
                    elif trail_active and pct <= trail_stop_pct:
                        reason = f"TRAIL STOP (locked in {trail_stop_pct:+.2f}%)"
                    elif pct <= -STOP_LOSS_PCT:
                        reason = "STOP LOSS"

                    if reason:
                        _exit(position_sym, position["dollars"], price, position["entry"], reason)
                        total_pnl += (price - position["entry"]) * position["qty"]
                        trade_count += 1
                        position = None
                        position_sym = None

            # --- Entry logic ---
            if position is None and not near_close:
                candidates = []
                for sym, price in prices.items():
                    if sym not in open_prices:
                        continue
                    pct = (price - open_prices[sym]) / open_prices[sym] * 100
                    if pct >= ENTRY_MOMENTUM_PCT:
                        candidates.append((pct, sym, price))

                candidates.sort(reverse=True)

                if candidates:
                    best_pct, best_sym, best_price = candidates[0]
                    log.info(f"ENTRY: {best_sym} +{best_pct:.2f}% — going all-in ${TOTAL_CAPITAL} @ ~${best_price:.2f}")
                    try:
                        order = place_order(best_sym, TOTAL_CAPITAL, "buy")
                        order_id = order.get("id") if order else None
                        if order_id:
                            qty = shares_for_amount(best_price, TOTAL_CAPITAL)
                            position = {"entry": best_price, "qty": qty, "dollars": TOTAL_CAPITAL, "peak": best_price}
                            position_sym = best_sym
                            log.info(f"  BUY order placed: {order_id}")
                        else:
                            log.warning(f"  Buy order failed: {order}")
                    except Exception as e:
                        log.warning(f"  Buy order error: {e}")
                else:
                    # Log all tickers so user can see what's happening
                    for sym, price in sorted(prices.items(), key=lambda x: -(x[1] - open_prices.get(x[0], x[1])) / open_prices.get(x[0], x[1])):
                        if sym in open_prices:
                            pct = (price - open_prices[sym]) / open_prices[sym] * 100
                            log.info(f"  {sym} ${price:.2f}  {pct:+.2f}%")

            time.sleep(POLL_SECONDS)

    except KeyboardInterrupt:
        log.info("Interrupted — emergency exit.")
        if position and position_sym:
            price = prices.get(position_sym, position["entry"])
            log.warning(f"Emergency exit: selling ${position['dollars']} of {position_sym}")
            try:
                place_order(position_sym, position["dollars"], "sell")
            except Exception as e:
                log.warning(f"  Emergency sell error: {e}")
            total_pnl += (price - position["entry"]) * position["qty"]

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
