import { NavigatorScreenParams } from '@react-navigation/native';

export type MainTabsParamList = {
  Dashboard: undefined;
  AIChat: undefined;
  Settings: undefined;
};

export type RootStackParamList = {
  Onboarding: undefined;
  MainTabs: NavigatorScreenParams<MainTabsParamList> | undefined;
  AddHabit: { habitId?: string } | undefined;
  HabitDetail: { habitId: string };
  Paywall: undefined;
};
