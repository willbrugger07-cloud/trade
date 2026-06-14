import { Completion } from '../types';
import { todayString, parseDateString, toDateString, addDays } from './date';

/**
 * Current streak: number of consecutive days (ending today or yesterday) that
 * have at least one completion for the habit.
 */
export function calcStreak(completions: Completion[]): number {
  const dates = new Set(completions.map((c) => c.date));
  if (dates.size === 0) return 0;

  let streak = 0;
  let cursor = parseDateString(todayString());

  // Allow the streak to count if today is not yet done but yesterday was.
  if (!dates.has(toDateString(cursor))) {
    cursor = addDays(cursor, -1);
    if (!dates.has(toDateString(cursor))) return 0;
  }

  while (dates.has(toDateString(cursor))) {
    streak += 1;
    cursor = addDays(cursor, -1);
  }
  return streak;
}

/** Longest streak ever recorded for the habit. */
export function calcBestStreak(completions: Completion[]): number {
  const sorted = Array.from(new Set(completions.map((c) => c.date))).sort();
  if (sorted.length === 0) return 0;

  let best = 1;
  let run = 1;
  for (let i = 1; i < sorted.length; i++) {
    const prev = parseDateString(sorted[i - 1]);
    const cur = parseDateString(sorted[i]);
    const diff = Math.round(
      (cur.getTime() - prev.getTime()) / (1000 * 60 * 60 * 24)
    );
    run = diff === 1 ? run + 1 : 1;
    if (run > best) best = run;
  }
  return best;
}

/** Days since the last completion (0 if completed today). Infinity if never. */
export function daysSinceLastCompletion(completions: Completion[]): number {
  if (completions.length === 0) return Infinity;
  const latest = completions
    .map((c) => c.date)
    .sort()
    .reverse()[0];
  const diff = Math.round(
    (parseDateString(todayString()).getTime() -
      parseDateString(latest).getTime()) /
      (1000 * 60 * 60 * 24)
  );
  return Math.max(0, diff);
}
