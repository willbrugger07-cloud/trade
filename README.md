# HabitCoach 🎯

A beautiful, production-ready habit tracker for iOS and Android, built with React
Native + Expo. Track daily habits, build streaks, visualize progress with a
GitHub-style heatmap, and get personalized coaching from an AI habit coach powered
by Claude.

## Features

- **Habit tracking** — daily, weekly, or X-times-per-week habits with emoji,
  categories, and reminder times.
- **Streaks & heatmaps** — consecutive-day streak tracking and a 12-week
  contribution heatmap per habit.
- **AI coaching** — chat with HabitCoach (Claude `claude-sonnet-4-6`) for daily
  check-ins, weekly summaries, and habit recommendations.
- **Smart reminders** — daily local notifications via Expo Notifications.
- **Premium subscriptions** — RevenueCat-style paywall (mocked) gating unlimited
  habits and unlimited AI chats. Free tier allows up to 3 habits.
- **Dark mode** — full light/dark theming that follows the system or a manual
  override.
- **Local-first** — all data stored on-device with AsyncStorage.

## Tech Stack

- React Native + Expo (SDK 51)
- TypeScript
- React Navigation (stack + bottom tabs)
- AsyncStorage for persistence
- Claude Messages API for AI coaching
- Expo Notifications for reminders
- RevenueCat-style subscription layer (mock implementation)

## Project Structure

```
src/
├── types/          Shared TypeScript types
├── constants/      Colors/theme, categories, config
├── storage/        AsyncStorage CRUD (habits, completions, conversations, settings)
├── services/       Claude API, notifications, subscriptions
├── hooks/          useHabits, useCompletions, useSubscription, useTheme
├── utils/          Date and streak helpers
├── components/     HabitCard, HeatmapCalendar, StreakBadge, CompletionAnimation,
│                   PaywallModal, CategoryBadge
├── navigation/     Navigation param types
└── screens/        Onboarding, Dashboard, AddHabit, HabitDetail, AIChat,
                    Settings, Paywall
App.tsx             Root: theme provider + navigation + onboarding gate
```

## Data model (AsyncStorage keys)

| Key             | Shape                                                  |
| --------------- | ------------------------------------------------------ |
| `habits`        | `Habit[]`                                              |
| `completions`   | `Completion[]`                                         |
| `conversations` | `AIConversation[]`                                     |
| `user_settings` | `{ isPremium, onboardingComplete, name, ... }`         |

## Getting Started

1. Install dependencies:

   ```bash
   npm install
   ```

2. Configure environment variables. Copy `.env.example` to `.env` and add your
   Claude API key:

   ```bash
   cp .env.example .env
   # then edit .env:
   # EXPO_PUBLIC_CLAUDE_API_KEY=sk-ant-...
   ```

   The key is read from `process.env.EXPO_PUBLIC_CLAUDE_API_KEY`. AI features show
   a friendly error if it is missing.

3. Start the dev server:

   ```bash
   npm start
   ```

   Then press `i` (iOS simulator), `a` (Android emulator), or scan the QR code
   with Expo Go.

## Notes

- **RevenueCat** is mocked because native subscription modules can't be linked in
  this environment. `src/services/revenuecat.ts` simulates purchase/restore and
  persists premium state locally — swap it for `react-native-purchases` to go
  live. The public API mirrors RevenueCat.
- **Streak calculation** counts consecutive days with at least one completion,
  ending today or yesterday.
- **Dates** are stored as `YYYY-MM-DD` strings in local time.

## Scripts

| Command         | Description                |
| --------------- | -------------------------- |
| `npm start`     | Start Expo dev server      |
| `npm run ios`   | Open in iOS simulator      |
| `npm run android` | Open in Android emulator |
| `npm run tsc`   | Type-check the project     |
