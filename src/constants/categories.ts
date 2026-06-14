import { Category } from '../types';
import { CATEGORY_COLORS } from './colors';

export interface CategoryMeta {
  key: Category;
  label: string;
  emoji: string;
  color: string;
}

export const CATEGORIES: CategoryMeta[] = [
  { key: 'health', label: 'Health', emoji: '🩺', color: CATEGORY_COLORS.health },
  { key: 'fitness', label: 'Fitness', emoji: '💪', color: CATEGORY_COLORS.fitness },
  {
    key: 'mindfulness',
    label: 'Mindfulness',
    emoji: '🧘',
    color: CATEGORY_COLORS.mindfulness,
  },
  {
    key: 'productivity',
    label: 'Productivity',
    emoji: '⚡',
    color: CATEGORY_COLORS.productivity,
  },
  { key: 'finance', label: 'Finance', emoji: '💰', color: CATEGORY_COLORS.finance },
];

export function getCategoryMeta(key: Category): CategoryMeta {
  return CATEGORIES.find((c) => c.key === key) ?? CATEGORIES[0];
}

/** Common emojis for the Add Habit emoji picker. */
export const EMOJI_OPTIONS = [
  '🏃', '🚶', '📚', '🧘', '💧', '📵', '💪', '✍️',
  '🥗', '😴', '🦷', '🧹', '☀️', '🌙', '🎯', '🎨',
  '🎸', '🧠', '💊', '🚴', '🏊', '🥦', '☕', '🚭',
  '💰', '📈', '🙏', '❤️', '🌱', '⏰', '📝', '🎧',
];

/** Curated starter habits for onboarding. */
export interface StarterHabit {
  name: string;
  emoji: string;
  category: Category;
}

export const STARTER_HABITS: StarterHabit[] = [
  { name: 'Morning Walk', emoji: '🚶', category: 'fitness' },
  { name: 'Read 30 min', emoji: '📚', category: 'productivity' },
  { name: 'Meditate', emoji: '🧘', category: 'mindfulness' },
  { name: 'Drink 8 glasses of water', emoji: '💧', category: 'health' },
  { name: 'No phone before bed', emoji: '📵', category: 'mindfulness' },
  { name: 'Exercise', emoji: '💪', category: 'fitness' },
  { name: 'Journal', emoji: '✍️', category: 'productivity' },
];
