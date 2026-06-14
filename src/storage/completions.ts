import AsyncStorage from '@react-native-async-storage/async-storage';
import { Completion } from '../types';
import { STORAGE_KEYS } from '../constants/config';

export async function getCompletions(): Promise<Completion[]> {
  try {
    const raw = await AsyncStorage.getItem(STORAGE_KEYS.completions);
    if (!raw) return [];
    const parsed = JSON.parse(raw);
    return Array.isArray(parsed) ? (parsed as Completion[]) : [];
  } catch (e) {
    console.warn('getCompletions failed', e);
    return [];
  }
}

async function persist(completions: Completion[]): Promise<void> {
  await AsyncStorage.setItem(
    STORAGE_KEYS.completions,
    JSON.stringify(completions)
  );
}

export async function saveCompletion(completion: Completion): Promise<void> {
  const completions = await getCompletions();
  // Guard against duplicate completion for the same habit + date.
  const exists = completions.some(
    (c) => c.habitId === completion.habitId && c.date === completion.date
  );
  if (exists) return;
  completions.push(completion);
  await persist(completions);
}

export async function getCompletionsForDate(
  date: string
): Promise<Completion[]> {
  const completions = await getCompletions();
  return completions.filter((c) => c.date === date);
}

export async function getCompletionsForHabit(
  habitId: string
): Promise<Completion[]> {
  const completions = await getCompletions();
  return completions.filter((c) => c.habitId === habitId);
}

export async function removeCompletion(
  habitId: string,
  date: string
): Promise<void> {
  const completions = await getCompletions();
  await persist(
    completions.filter((c) => !(c.habitId === habitId && c.date === date))
  );
}

/** Remove all completions belonging to a habit (used when deleting a habit). */
export async function removeCompletionsForHabit(
  habitId: string
): Promise<void> {
  const completions = await getCompletions();
  await persist(completions.filter((c) => c.habitId !== habitId));
}
