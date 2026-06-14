import React, { useState } from 'react';
import {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  StyleSheet,
  ScrollView,
  SafeAreaView,
} from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { useTheme } from '../hooks/useTheme';
import { STARTER_HABITS, StarterHabit } from '../constants/categories';
import { useHabits } from '../hooks/useHabits';
import { updateSettings } from '../storage/settings';
import {
  requestPermissions,
  scheduleStruggleCheck,
} from '../services/notifications';
import { PALETTE } from '../constants/colors';

interface Props {
  onComplete: () => void;
}

export default function OnboardingScreen({ onComplete }: Props) {
  const theme = useTheme();
  const { addHabit } = useHabits();
  const [step, setStep] = useState(0);
  const [name, setName] = useState('');
  const [selected, setSelected] = useState<StarterHabit[]>([]);
  const [reminderTime, setReminderTime] = useState('09:00');
  const [saving, setSaving] = useState(false);

  const toggleStarter = (habit: StarterHabit) => {
    setSelected((prev) => {
      const exists = prev.find((h) => h.name === habit.name);
      if (exists) return prev.filter((h) => h.name !== habit.name);
      if (prev.length >= 3) return prev;
      return [...prev, habit];
    });
  };

  const finish = async () => {
    setSaving(true);
    try {
      await updateSettings({
        name: name.trim() || 'Friend',
        onboardingComplete: true,
        notificationsEnabled: true,
      });
      const granted = await requestPermissions().catch(() => false);
      for (const h of selected) {
        await addHabit({
          name: h.name,
          emoji: h.emoji,
          category: h.category,
          frequency: 'daily',
          reminderTime,
        });
      }
      if (granted) {
        await scheduleStruggleCheck().catch(() => {});
      }
      onComplete();
    } finally {
      setSaving(false);
    }
  };

  const canContinue =
    step === 0 ? name.trim().length > 0 : step === 1 ? selected.length > 0 : true;

  return (
    <View style={[styles.root, { backgroundColor: theme.background }]}>
      <LinearGradient
        colors={[theme.primary, '#8B82FF']}
        style={styles.header}
      >
        <SafeAreaView>
          <Text style={styles.logo}>HabitCoach</Text>
          <View style={styles.dots}>
            {[0, 1, 2].map((i) => (
              <View
                key={i}
                style={[
                  styles.dot,
                  { backgroundColor: i <= step ? PALETTE.white : '#ffffff55' },
                ]}
              />
            ))}
          </View>
        </SafeAreaView>
      </LinearGradient>

      <ScrollView
        contentContainerStyle={styles.body}
        keyboardShouldPersistTaps="handled"
      >
        {step === 0 && (
          <View>
            <Text style={[styles.title, { color: theme.text }]}>
              Welcome! 👋
            </Text>
            <Text style={[styles.subtitle, { color: theme.textSecondary }]}>
              Your friendly AI coach for building habits that actually stick.
              What should we call you?
            </Text>
            <TextInput
              value={name}
              onChangeText={setName}
              placeholder="Your name"
              placeholderTextColor={theme.textSecondary}
              style={[
                styles.input,
                {
                  backgroundColor: theme.card,
                  color: theme.text,
                  borderColor: theme.border,
                },
              ]}
              autoFocus
              returnKeyType="next"
            />
          </View>
        )}

        {step === 1 && (
          <View>
            <Text style={[styles.title, { color: theme.text }]}>
              Pick your starters
            </Text>
            <Text style={[styles.subtitle, { color: theme.textSecondary }]}>
              Choose 1–3 habits to begin with. You can add more anytime.
            </Text>
            {STARTER_HABITS.map((h) => {
              const isSel = !!selected.find((s) => s.name === h.name);
              return (
                <TouchableOpacity
                  key={h.name}
                  onPress={() => toggleStarter(h)}
                  style={[
                    styles.starter,
                    {
                      backgroundColor: theme.card,
                      borderColor: isSel ? theme.primary : theme.border,
                      borderWidth: isSel ? 2 : 1,
                    },
                  ]}
                >
                  <Text style={styles.starterEmoji}>{h.emoji}</Text>
                  <Text style={[styles.starterName, { color: theme.text }]}>
                    {h.name}
                  </Text>
                  <View
                    style={[
                      styles.check,
                      {
                        backgroundColor: isSel ? theme.primary : 'transparent',
                        borderColor: isSel ? theme.primary : theme.border,
                      },
                    ]}
                  >
                    {isSel && <Text style={styles.checkMark}>✓</Text>}
                  </View>
                </TouchableOpacity>
              );
            })}
          </View>
        )}

        {step === 2 && (
          <View>
            <Text style={[styles.title, { color: theme.text }]}>
              Stay on track
            </Text>
            <Text style={[styles.subtitle, { color: theme.textSecondary }]}>
              When should we remind you each day?
            </Text>
            <View style={styles.timeRow}>
              {['07:00', '09:00', '12:00', '18:00', '21:00'].map((t) => (
                <TouchableOpacity
                  key={t}
                  onPress={() => setReminderTime(t)}
                  style={[
                    styles.timeChip,
                    {
                      backgroundColor:
                        reminderTime === t ? theme.primary : theme.card,
                      borderColor:
                        reminderTime === t ? theme.primary : theme.border,
                    },
                  ]}
                >
                  <Text
                    style={{
                      color: reminderTime === t ? PALETTE.white : theme.text,
                      fontWeight: '600',
                    }}
                  >
                    {t}
                  </Text>
                </TouchableOpacity>
              ))}
            </View>

            <View
              style={[
                styles.planBox,
                { backgroundColor: theme.card, borderColor: theme.border },
              ]}
            >
              <Text style={[styles.planTitle, { color: theme.text }]}>
                Free vs Premium
              </Text>
              <Text style={[styles.planLine, { color: theme.textSecondary }]}>
                ✓ Free: up to 3 habits, streaks & reminders
              </Text>
              <Text style={[styles.planLine, { color: theme.textSecondary }]}>
                ⭐ Premium: unlimited habits + AI coaching
              </Text>
            </View>
          </View>
        )}
      </ScrollView>

      <SafeAreaView style={styles.footer}>
        <View style={styles.footerRow}>
          {step > 0 && (
            <TouchableOpacity
              onPress={() => setStep((s) => s - 1)}
              style={[styles.backBtn, { borderColor: theme.border }]}
            >
              <Text style={{ color: theme.text, fontWeight: '600' }}>Back</Text>
            </TouchableOpacity>
          )}
          <TouchableOpacity
            disabled={!canContinue || saving}
            onPress={() => (step < 2 ? setStep((s) => s + 1) : finish())}
            style={[
              styles.nextBtn,
              {
                backgroundColor: theme.primary,
                opacity: !canContinue || saving ? 0.5 : 1,
              },
            ]}
          >
            <Text style={styles.nextText}>
              {saving ? 'Setting up…' : step < 2 ? 'Next' : 'Get started'}
            </Text>
          </TouchableOpacity>
        </View>
      </SafeAreaView>
    </View>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1 },
  header: { paddingBottom: 24, paddingHorizontal: 24 },
  logo: {
    color: PALETTE.white,
    fontSize: 26,
    fontWeight: '800',
    marginTop: 12,
  },
  dots: { flexDirection: 'row', marginTop: 16, gap: 8 },
  dot: { width: 28, height: 6, borderRadius: 3 },
  body: { padding: 24 },
  title: { fontSize: 28, fontWeight: '800', marginBottom: 8 },
  subtitle: { fontSize: 16, lineHeight: 22, marginBottom: 24 },
  input: {
    borderWidth: 1,
    borderRadius: 14,
    paddingHorizontal: 16,
    paddingVertical: 14,
    fontSize: 18,
  },
  starter: {
    flexDirection: 'row',
    alignItems: 'center',
    borderRadius: 14,
    padding: 16,
    marginBottom: 12,
  },
  starterEmoji: { fontSize: 24, marginRight: 14 },
  starterName: { flex: 1, fontSize: 16, fontWeight: '600' },
  check: {
    width: 26,
    height: 26,
    borderRadius: 13,
    borderWidth: 2,
    alignItems: 'center',
    justifyContent: 'center',
  },
  checkMark: { color: PALETTE.white, fontWeight: '800' },
  timeRow: { flexDirection: 'row', flexWrap: 'wrap', gap: 10, marginBottom: 24 },
  timeChip: {
    paddingHorizontal: 16,
    paddingVertical: 10,
    borderRadius: 12,
    borderWidth: 1,
  },
  planBox: { borderWidth: 1, borderRadius: 14, padding: 16 },
  planTitle: { fontSize: 16, fontWeight: '700', marginBottom: 10 },
  planLine: { fontSize: 14, lineHeight: 22 },
  footer: { paddingHorizontal: 24 },
  footerRow: { flexDirection: 'row', gap: 12, paddingBottom: 12 },
  backBtn: {
    paddingHorizontal: 24,
    paddingVertical: 16,
    borderRadius: 14,
    borderWidth: 1,
    justifyContent: 'center',
  },
  nextBtn: {
    flex: 1,
    paddingVertical: 16,
    borderRadius: 14,
    alignItems: 'center',
  },
  nextText: { color: PALETTE.white, fontSize: 17, fontWeight: '700' },
});
