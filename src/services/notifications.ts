import * as Notifications from 'expo-notifications';
import { Platform } from 'react-native';
import { Habit } from '../types';

Notifications.setNotificationHandler({
  handleNotification: async () => ({
    shouldShowAlert: true,
    shouldPlaySound: true,
    shouldSetBadge: false,
  }),
});

/** Map of habitId -> scheduled notification identifier (in-memory cache). */
const scheduledIds = new Map<string, string>();

export async function requestPermissions(): Promise<boolean> {
  const { status: existing } = await Notifications.getPermissionsAsync();
  let status = existing;
  if (existing !== 'granted') {
    const res = await Notifications.requestPermissionsAsync();
    status = res.status;
  }

  if (Platform.OS === 'android') {
    await Notifications.setNotificationChannelAsync('habit-reminders', {
      name: 'Habit Reminders',
      importance: Notifications.AndroidImportance.DEFAULT,
      vibrationPattern: [0, 250, 250, 250],
      lightColor: '#6C63FF',
    });
  }

  return status === 'granted';
}

function parseTime(time: string): { hour: number; minute: number } {
  const [h, m] = time.split(':').map(Number);
  return { hour: h || 9, minute: m || 0 };
}

/** Schedule a repeating daily reminder for a habit. Returns the notif id. */
export async function scheduleHabitReminder(habit: Habit): Promise<string> {
  await cancelHabitReminder(habit.id);
  if (!habit.reminderTime) return '';

  const { hour, minute } = parseTime(habit.reminderTime);
  const id = await Notifications.scheduleNotificationAsync({
    content: {
      title: `${habit.emoji} ${habit.name}`,
      body: "Time to keep your streak alive — you've got this!",
      data: { habitId: habit.id },
    },
    trigger: {
      hour,
      minute,
      repeats: true,
      channelId: 'habit-reminders',
    },
  });
  scheduledIds.set(habit.id, id);
  return id;
}

export async function cancelHabitReminder(habitId: string): Promise<void> {
  const cached = scheduledIds.get(habitId);
  if (cached) {
    await Notifications.cancelScheduledNotificationAsync(cached).catch(() => {});
    scheduledIds.delete(habitId);
    return;
  }
  // Fallback: scan all scheduled notifications for this habit.
  const all = await Notifications.getAllScheduledNotificationsAsync();
  await Promise.all(
    all
      .filter((n) => n.content.data?.habitId === habitId)
      .map((n) =>
        Notifications.cancelScheduledNotificationAsync(n.identifier).catch(
          () => {}
        )
      )
  );
}

export async function cancelAllReminders(): Promise<void> {
  await Notifications.cancelAllScheduledNotificationsAsync().catch(() => {});
  scheduledIds.clear();
}

/**
 * Schedule a daily background check that nudges the user if habits are being
 * missed. Fires once per day in the evening as a self-accountability prompt.
 */
export async function scheduleStruggleCheck(): Promise<string> {
  return Notifications.scheduleNotificationAsync({
    content: {
      title: 'How are your habits going? 🌙',
      body: 'Open HabitCoach to check in and keep your momentum.',
      data: { type: 'struggle-check' },
    },
    trigger: {
      hour: 20,
      minute: 0,
      repeats: true,
      channelId: 'habit-reminders',
    },
  });
}
