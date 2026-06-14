import { useCallback, useEffect, useState } from 'react';
import { v4 as uuidv4 } from 'uuid';
import { Habit, Completion } from '../types';
import {
  getHabits,
  saveHabit as persistHabit,
  updateHabit as persistUpdate,
  deleteHabit as persistDelete,
} from '../storage/habits';
import {
  getCompletions,
  saveCompletion,
  removeCompletion,
  removeCompletionsForHabit,
} from '../storage/completions';
import { calcStreak } from '../utils/streak';
import { todayString } from '../utils/date';
import {
  scheduleHabitReminder,
  cancelHabitReminder,
} from '../services/notifications';

export interface UseHabits {
  habits: Habit[];
  completions: Completion[];
  loading: boolean;
  refresh: () => Promise<void>;
  addHabit: (habit: Omit<Habit, 'id' | 'createdAt'>) => Promise<Habit>;
  editHabit: (habit: Habit) => Promise<void>;
  deleteHabit: (id: string) => Promise<void>;
  toggleComplete: (habitId: string, date?: string) => Promise<void>;
  isCompleted: (habitId: string, date?: string) => boolean;
  getStreakForHabit: (habitId: string) => number;
  getTodayCompletions: () => Completion[];
}

export function useHabits(): UseHabits {
  const [habits, setHabits] = useState<Habit[]>([]);
  const [completions, setCompletions] = useState<Completion[]>([]);
  const [loading, setLoading] = useState(true);

  const refresh = useCallback(async () => {
    const [h, c] = await Promise.all([getHabits(), getCompletions()]);
    setHabits(h);
    setCompletions(c);
    setLoading(false);
  }, []);

  useEffect(() => {
    refresh();
  }, [refresh]);

  const addHabit = useCallback(
    async (habit: Omit<Habit, 'id' | 'createdAt'>) => {
      const full: Habit = {
        ...habit,
        id: uuidv4(),
        createdAt: new Date().toISOString(),
      };
      await persistHabit(full);
      if (full.reminderTime) {
        await scheduleHabitReminder(full).catch(() => {});
      }
      await refresh();
      return full;
    },
    [refresh]
  );

  const editHabit = useCallback(
    async (habit: Habit) => {
      await persistUpdate(habit);
      await cancelHabitReminder(habit.id).catch(() => {});
      if (habit.reminderTime) {
        await scheduleHabitReminder(habit).catch(() => {});
      }
      await refresh();
    },
    [refresh]
  );

  const deleteHabit = useCallback(
    async (id: string) => {
      await cancelHabitReminder(id).catch(() => {});
      await persistDelete(id);
      await removeCompletionsForHabit(id);
      await refresh();
    },
    [refresh]
  );

  const isCompleted = useCallback(
    (habitId: string, date: string = todayString()) =>
      completions.some((c) => c.habitId === habitId && c.date === date),
    [completions]
  );

  const toggleComplete = useCallback(
    async (habitId: string, date: string = todayString()) => {
      if (isCompleted(habitId, date)) {
        await removeCompletion(habitId, date);
      } else {
        await saveCompletion({
          id: uuidv4(),
          habitId,
          date,
          completedAt: new Date().toISOString(),
        });
      }
      await refresh();
    },
    [isCompleted, refresh]
  );

  const getStreakForHabit = useCallback(
    (habitId: string) =>
      calcStreak(completions.filter((c) => c.habitId === habitId)),
    [completions]
  );

  const getTodayCompletions = useCallback(
    () => completions.filter((c) => c.date === todayString()),
    [completions]
  );

  return {
    habits,
    completions,
    loading,
    refresh,
    addHabit,
    editHabit,
    deleteHabit,
    toggleComplete,
    isCompleted,
    getStreakForHabit,
    getTodayCompletions,
  };
}
