export const CLAUDE_API_URL = 'https://api.anthropic.com/v1/messages';
export const CLAUDE_MODEL = 'claude-sonnet-4-6';
export const CLAUDE_API_VERSION = '2023-06-01';
export const CLAUDE_MAX_TOKENS = 1024;

export const CLAUDE_SYSTEM_PROMPT =
  'You are HabitCoach, a warm, encouraging, and practical habit coach. ' +
  'Your job is to help users build lasting habits through accountability and ' +
  'positive reinforcement. Keep responses conversational, under 150 words, and ' +
  'always end with one specific actionable tip or question. Never be preachy. ' +
  'Be supportive like a knowledgeable friend.';

export const STORAGE_KEYS = {
  habits: 'habits',
  completions: 'completions',
  conversations: 'conversations',
  settings: 'user_settings',
} as const;

export const PRICING = {
  monthly: '$4.99/month',
  yearly: '$34.99/year',
  monthlyValue: 4.99,
  yearlyValue: 34.99,
};

export const PREMIUM_FEATURES = [
  'Unlimited habits',
  'AI habit coaching & daily check-ins',
  'Weekly AI summaries',
  'Smart struggle nudges',
  'Advanced analytics & heatmaps',
  'Priority support',
];
