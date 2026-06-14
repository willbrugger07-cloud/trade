import { useCallback, useEffect, useState } from 'react';
import { Completion } from '../types';
import {
  getCompletions,
  getCompletionsForHabit,
} from '../storage/completions';

export interface UseCompletions {
  completions: Completion[];
  loading: boolean;
  refresh: () => Promise<void>;
  forHabit: (habitId: string) => Completion[];
}

/** Read-only hook over all completion records. */
export function useCompletions(): UseCompletions {
  const [completions, setCompletions] = useState<Completion[]>([]);
  const [loading, setLoading] = useState(true);

  const refresh = useCallback(async () => {
    const all = await getCompletions();
    setCompletions(all);
    setLoading(false);
  }, []);

  useEffect(() => {
    refresh();
  }, [refresh]);

  const forHabit = useCallback(
    (habitId: string) => completions.filter((c) => c.habitId === habitId),
    [completions]
  );

  return { completions, loading, refresh, forHabit };
}

/** Convenience hook scoped to a single habit's completions. */
export function useHabitCompletions(habitId: string) {
  const [completions, setCompletions] = useState<Completion[]>([]);
  const [loading, setLoading] = useState(true);

  const refresh = useCallback(async () => {
    setCompletions(await getCompletionsForHabit(habitId));
    setLoading(false);
  }, [habitId]);

  useEffect(() => {
    refresh();
  }, [refresh]);

  return { completions, loading, refresh };
}
