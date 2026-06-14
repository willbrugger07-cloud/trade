export type Category =
  | 'health'
  | 'fitness'
  | 'mindfulness'
  | 'productivity'
  | 'finance';

export type Frequency = 'daily' | 'weekly' | { timesPerWeek: number };

export interface Habit {
  id: string;
  name: string;
  emoji: string;
  category: Category;
  frequency: Frequency;
  reminderTime?: string; // HH:MM format
  createdAt: string;
}

export interface Completion {
  id: string;
  habitId: string;
  completedAt: string;
  date: string; // YYYY-MM-DD
}

export interface AIConversation {
  id: string;
  date: string;
  userMessage: string;
  aiResponse: string;
}

export interface UserSettings {
  isPremium: boolean;
  onboardingComplete: boolean;
  name: string;
  notificationsEnabled?: boolean;
  darkModeOverride?: 'light' | 'dark' | null;
}

export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
}

/** Helper type guard for the structured frequency variant. */
export function isTimesPerWeek(
  frequency: Frequency
): frequency is { timesPerWeek: number } {
  return typeof frequency === 'object' && 'timesPerWeek' in frequency;
}

/** Human-readable label for a frequency value. */
export function frequencyLabel(frequency: Frequency): string {
  if (frequency === 'daily') return 'Daily';
  if (frequency === 'weekly') return 'Weekly';
  return `${frequency.timesPerWeek}x / week`;
}
