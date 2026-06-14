import AsyncStorage from '@react-native-async-storage/async-storage';
import { Habit } from '../types';
import { STORAGE_KEYS } from '../constants/config';

export async function getHabits(): Promise<Habit[]> {
  try {
    const raw = await AsyncStorage.getItem(STORAGE_KEYS.habits);
    if (!raw) return [];
    const parsed = JSON.parse(raw);
    return Array.isArray(parsed) ? (parsed as Habit[]) : [];
  } catch (e) {
    console.warn('getHabits failed', e);
    return [];
  }
}

async function persist(habits: Habit[]): Promise<void> {
  await AsyncStorage.setItem(STORAGE_KEYS.habits, JSON.stringify(habits));
}

export async function saveHabit(habit: Habit): Promise<void> {
  const habits = await getHabits();
  habits.push(habit);
  await persist(habits);
}

export async function updateHabit(habit: Habit): Promise<void> {
  const habits = await getHabits();
  const idx = habits.findIndex((h) => h.id === habit.id);
  if (idx === -1) {
    habits.push(habit);
  } else {
    habits[idx] = habit;
  }
  await persist(habits);
}

export async function deleteHabit(id: string): Promise<void> {
  const habits = await getHabits();
  await persist(habits.filter((h) => h.id !== id));
}

export async function getHabitById(id: string): Promise<Habit | undefined> {
  const habits = await getHabits();
  return habits.find((h) => h.id === id);
}
