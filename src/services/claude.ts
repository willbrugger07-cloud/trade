import { Habit, Completion, ChatMessage, frequencyLabel } from '../types';
import {
  CLAUDE_API_URL,
  CLAUDE_MODEL,
  CLAUDE_API_VERSION,
  CLAUDE_MAX_TOKENS,
  CLAUDE_SYSTEM_PROMPT,
} from '../constants/config';
import { calcStreak } from '../utils/streak';
import { getCompletionsForHabit } from '../storage/completions';

const API_KEY = process.env.EXPO_PUBLIC_CLAUDE_API_KEY ?? '';

/**
 * Low-level call to the Claude Messages API.
 * React Native has no official Anthropic SDK, so we use fetch directly.
 */
async function callClaude(
  messages: ChatMessage[],
  systemOverride?: string
): Promise<string> {
  if (!API_KEY) {
    throw new Error(
      'Missing Claude API key. Set EXPO_PUBLIC_CLAUDE_API_KEY in your .env file.'
    );
  }

  const response = await fetch(CLAUDE_API_URL, {
    method: 'POST',
    headers: {
      'content-type': 'application/json',
      'x-api-key': API_KEY,
      'anthropic-version': CLAUDE_API_VERSION,
    },
    body: JSON.stringify({
      model: CLAUDE_MODEL,
      max_tokens: CLAUDE_MAX_TOKENS,
      system: systemOverride ?? CLAUDE_SYSTEM_PROMPT,
      messages: messages.map((m) => ({ role: m.role, content: m.content })),
    }),
  });

  if (!response.ok) {
    const detail = await response.text().catch(() => '');
    throw new Error(
      `Claude API error (${response.status}): ${detail || response.statusText}`
    );
  }

  const data = await response.json();
  const text = Array.isArray(data?.content)
    ? data.content
        .filter((b: { type: string }) => b.type === 'text')
        .map((b: { text: string }) => b.text)
        .join('\n')
        .trim()
    : '';
  return text || "I'm here for you — could you say that again?";
}

/** Send a chat message with prior conversation history. */
export async function sendMessage(
  userMessage: string,
  conversationHistory: ChatMessage[]
): Promise<string> {
  const messages: ChatMessage[] = [
    ...conversationHistory,
    { role: 'user', content: userMessage },
  ];
  return callClaude(messages);
}

/** Build a compact text summary of habit progress for prompting. */
function summarizeProgress(habits: Habit[], completions: Completion[]): string {
  if (habits.length === 0) return 'The user has no habits yet.';
  const today = new Date().toISOString().slice(0, 10);
  return habits
    .map((h) => {
      const forHabit = completions.filter((c) => c.habitId === h.id);
      const streak = calcStreak(forHabit);
      const doneToday = forHabit.some((c) => c.date === today);
      return `- ${h.emoji} ${h.name} (${frequencyLabel(
        h.frequency
      )}): ${streak}-day streak, ${doneToday ? 'done today' : 'not done today'}`;
    })
    .join('\n');
}

export async function getDailyCheckin(
  habits: Habit[],
  completions: Completion[]
): Promise<string> {
  const summary = summarizeProgress(habits, completions);
  const prompt =
    `Here is the user's habit progress today:\n${summary}\n\n` +
    'Give a short, warm daily check-in that celebrates wins and gently ' +
    'encourages anything not yet done.';
  return callClaude([{ role: 'user', content: prompt }]);
}

export async function getWeeklySummary(
  habits: Habit[],
  completions: Completion[]
): Promise<string> {
  const summary = summarizeProgress(habits, completions);
  const weekAgo = new Date(Date.now() - 7 * 86400000)
    .toISOString()
    .slice(0, 10);
  const weekCount = completions.filter((c) => c.date >= weekAgo).length;
  const prompt =
    `Here is the user's habit overview:\n${summary}\n\n` +
    `They completed ${weekCount} habit check-ins over the past 7 days.\n\n` +
    'Give an encouraging weekly summary highlighting trends and one focus ' +
    'area for next week.';
  return callClaude([{ role: 'user', content: prompt }]);
}

export async function getStruggleNudge(
  habit: Habit,
  missedDays: number
): Promise<string> {
  const prompt =
    `The user has missed their habit "${habit.emoji} ${habit.name}" for ` +
    `${missedDays} day(s). Give a gentle, non-judgmental nudge to help them ` +
    'get back on track.';
  return callClaude([{ role: 'user', content: prompt }]);
}

export async function getHabitRecommendations(
  habits: Habit[]
): Promise<string> {
  const existing =
    habits.length > 0
      ? habits.map((h) => `${h.emoji} ${h.name}`).join(', ')
      : 'none yet';
  const prompt =
    `The user currently tracks these habits: ${existing}.\n\n` +
    'Suggest 1-2 complementary new habits that would pair well, with a brief ' +
    'reason for each.';
  return callClaude([{ role: 'user', content: prompt }]);
}

/** Ask the coach a question scoped to a single habit (premium feature). */
export async function askAboutHabit(habit: Habit): Promise<string> {
  const completions = await getCompletionsForHabit(habit.id);
  const streak = calcStreak(completions);
  const prompt =
    `Let's talk about my habit "${habit.emoji} ${habit.name}" ` +
    `(${frequencyLabel(habit.frequency)}). My current streak is ${streak} ` +
    `day(s) and I have ${completions.length} total completions. ` +
    'How am I doing and what should I focus on?';
  return callClaude([{ role: 'user', content: prompt }]);
}
