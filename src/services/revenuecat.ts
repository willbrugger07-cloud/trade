/**
 * Mock RevenueCat integration.
 *
 * Native subscription SDKs (react-native-purchases) require native modules that
 * cannot be linked in this environment, so this module simulates the purchase
 * flow and persists premium state locally via the settings store. The public
 * API mirrors RevenueCat so it can be swapped for the real SDK later.
 */
import { getSettings, updateSettings } from '../storage/settings';

export const FREE_HABIT_LIMIT = 3;

export async function isPremium(): Promise<boolean> {
  const settings = await getSettings();
  return settings.isPremium;
}

/** Simulate a purchase. Resolves true on success. */
export async function purchasePremium(): Promise<boolean> {
  // Simulate network/store latency.
  await new Promise((r) => setTimeout(r, 800));
  await updateSettings({ isPremium: true });
  return true;
}

/** Simulate restoring a prior purchase. */
export async function restorePurchases(): Promise<boolean> {
  await new Promise((r) => setTimeout(r, 600));
  const settings = await getSettings();
  // In the mock, "restore" simply re-reports current local state.
  return settings.isPremium;
}

/** Allow downgrade (useful for testing / managing subscription). */
export async function cancelPremium(): Promise<void> {
  await updateSettings({ isPremium: false });
}
