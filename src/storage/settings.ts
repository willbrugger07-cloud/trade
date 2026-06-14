import AsyncStorage from '@react-native-async-storage/async-storage';
import { UserSettings } from '../types';
import { STORAGE_KEYS } from '../constants/config';

export const DEFAULT_SETTINGS: UserSettings = {
  isPremium: false,
  onboardingComplete: false,
  name: '',
  notificationsEnabled: true,
  darkModeOverride: null,
};

export async function getSettings(): Promise<UserSettings> {
  try {
    const raw = await AsyncStorage.getItem(STORAGE_KEYS.settings);
    if (!raw) return { ...DEFAULT_SETTINGS };
    const parsed = JSON.parse(raw);
    return { ...DEFAULT_SETTINGS, ...parsed } as UserSettings;
  } catch (e) {
    console.warn('getSettings failed', e);
    return { ...DEFAULT_SETTINGS };
  }
}

export async function saveSettings(settings: UserSettings): Promise<void> {
  await AsyncStorage.setItem(STORAGE_KEYS.settings, JSON.stringify(settings));
}

export async function updateSettings(
  patch: Partial<UserSettings>
): Promise<UserSettings> {
  const current = await getSettings();
  const next = { ...current, ...patch };
  await saveSettings(next);
  return next;
}
