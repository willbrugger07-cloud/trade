import React, { useCallback, useState } from 'react';
import {
  ActivityIndicator,
  Alert,
  ScrollView,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { StackScreenProps } from '@react-navigation/stack';
import { RootStackParamList } from '../navigation/types';
import { useTheme } from '../hooks/useTheme';
import { useHabits } from '../hooks/useHabits';
import { useHabitCompletions } from '../hooks/useCompletions';
import { useSubscription } from '../hooks/useSubscription';
import { askAboutHabit } from '../services/claude';
import { calcStreak, calcBestStreak } from '../utils/streak';
import { frequencyLabel } from '../types';
import HeatmapCalendar from '../components/HeatmapCalendar';
import StreakBadge from '../components/StreakBadge';
import CategoryBadge from '../components/CategoryBadge';

type Props = StackScreenProps<RootStackParamList, 'HabitDetail'>;

function completionRateThisMonth(dates: string[]): number {
  const now = new Date();
  const year = now.getFullYear();
  const month = now.getMonth();
  const daysElapsed = now.getDate();
  const prefix = `${year}-${String(month + 1).padStart(2, '0')}`;
  const inMonth = dates.filter((d) => d.startsWith(prefix)).length;
  if (daysElapsed === 0) return 0;
  return Math.round((inMonth / daysElapsed) * 100);
}

export default function HabitDetailScreen({ navigation, route }: Props) {
  const theme = useTheme();
  const { habitId } = route.params;
  const { habits, loading: habitsLoading, deleteHabit } = useHabits();
  const { completions, loading: completionsLoading } =
    useHabitCompletions(habitId);
  const { isPremium } = useSubscription();

  const [aiResponse, setAiResponse] = useState<string | null>(null);
  const [aiLoading, setAiLoading] = useState(false);

  const habit = habits.find((h) => h.id === habitId);

  const handleAsk = useCallback(async () => {
    if (!habit) return;
    setAiLoading(true);
    setAiResponse(null);
    try {
      const res = await askAboutHabit(habit);
      setAiResponse(res);
    } catch (e) {
      setAiResponse(
        "Sorry, I couldn't reach your AI coach right now. Please try again later."
      );
    } finally {
      setAiLoading(false);
    }
  }, [habit]);

  const handleDelete = useCallback(() => {
    Alert.alert(
      'Delete habit',
      'Are you sure you want to delete this habit? This cannot be undone.',
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Delete',
          style: 'destructive',
          onPress: async () => {
            await deleteHabit(habitId);
            navigation.goBack();
          },
        },
      ]
    );
  }, [deleteHabit, habitId, navigation]);

  if (habitsLoading) {
    return (
      <SafeAreaView
        style={[styles.center, { backgroundColor: theme.background }]}
      >
        <ActivityIndicator color={theme.primary} />
      </SafeAreaView>
    );
  }

  if (!habit) {
    return (
      <SafeAreaView
        style={[styles.center, { backgroundColor: theme.background }]}
      >
        <Text style={[styles.notFoundEmoji]}>🤔</Text>
        <Text style={[styles.notFoundText, { color: theme.text }]}>
          This habit could not be found.
        </Text>
        <TouchableOpacity
          style={[styles.backBtn, { backgroundColor: theme.primary }]}
          onPress={() => navigation.goBack()}
        >
          <Text style={[styles.backBtnText, { color: theme.white }]}>
            Go back
          </Text>
        </TouchableOpacity>
      </SafeAreaView>
    );
  }

  const currentStreak = calcStreak(completions);
  const bestStreak = calcBestStreak(completions);
  const rate = completionRateThisMonth(completions.map((c) => c.date));

  return (
    <SafeAreaView style={[styles.flex, { backgroundColor: theme.background }]}>
      <ScrollView contentContainerStyle={styles.content}>
        <View style={styles.headerRow}>
          <TouchableOpacity onPress={() => navigation.goBack()}>
            <Text style={[styles.navBack, { color: theme.primary }]}>
              ‹ Back
            </Text>
          </TouchableOpacity>
        </View>

        <View style={styles.titleBlock}>
          <Text style={styles.emoji}>{habit.emoji}</Text>
          <Text style={[styles.name, { color: theme.text }]}>{habit.name}</Text>
          <View style={styles.badgeRow}>
            <CategoryBadge category={habit.category} />
            <StreakBadge streak={currentStreak} size="md" />
          </View>
          <Text style={[styles.frequency, { color: theme.textSecondary }]}>
            {frequencyLabel(habit.frequency)}
          </Text>
        </View>

        <View
          style={[
            styles.card,
            { backgroundColor: theme.card, borderColor: theme.border },
          ]}
        >
          {completionsLoading ? (
            <ActivityIndicator color={theme.primary} />
          ) : (
            <HeatmapCalendar completions={completions} />
          )}
        </View>

        <View style={styles.statsRow}>
          <View
            style={[
              styles.statCard,
              { backgroundColor: theme.card, borderColor: theme.border },
            ]}
          >
            <Text style={[styles.statValue, { color: theme.text }]}>
              {completions.length}
            </Text>
            <Text style={[styles.statLabel, { color: theme.textSecondary }]}>
              Total
            </Text>
          </View>
          <View
            style={[
              styles.statCard,
              { backgroundColor: theme.card, borderColor: theme.border },
            ]}
          >
            <Text style={[styles.statValue, { color: theme.streak }]}>
              {bestStreak}
            </Text>
            <Text style={[styles.statLabel, { color: theme.textSecondary }]}>
              Best streak
            </Text>
          </View>
          <View
            style={[
              styles.statCard,
              { backgroundColor: theme.card, borderColor: theme.border },
            ]}
          >
            <Text style={[styles.statValue, { color: theme.success }]}>
              {rate}%
            </Text>
            <Text style={[styles.statLabel, { color: theme.textSecondary }]}>
              This month
            </Text>
          </View>
        </View>

        {isPremium ? (
          <View style={styles.aiSection}>
            <TouchableOpacity
              style={[styles.aiButton, { backgroundColor: theme.primary }]}
              onPress={handleAsk}
              disabled={aiLoading}
            >
              {aiLoading ? (
                <ActivityIndicator color={theme.white} />
              ) : (
                <Text style={[styles.aiButtonText, { color: theme.white }]}>
                  Ask AI about this habit ✨
                </Text>
              )}
            </TouchableOpacity>
            {aiResponse && (
              <View
                style={[
                  styles.card,
                  { backgroundColor: theme.cardElevated, borderColor: theme.border },
                ]}
              >
                <Text style={[styles.aiResponse, { color: theme.text }]}>
                  {aiResponse}
                </Text>
              </View>
            )}
          </View>
        ) : (
          <Text style={[styles.upgradeNote, { color: theme.textSecondary }]}>
            ✨ Upgrade to Premium to ask AI about this habit
          </Text>
        )}

        <View style={styles.actions}>
          <TouchableOpacity
            style={[
              styles.actionBtn,
              { backgroundColor: theme.cardElevated, borderColor: theme.border },
            ]}
            onPress={() => navigation.navigate('AddHabit', { habitId })}
          >
            <Text style={[styles.actionText, { color: theme.text }]}>
              Edit habit
            </Text>
          </TouchableOpacity>
          <TouchableOpacity
            style={[styles.actionBtn, { borderColor: theme.danger }]}
            onPress={handleDelete}
          >
            <Text style={[styles.actionText, { color: theme.danger }]}>
              Delete habit
            </Text>
          </TouchableOpacity>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  center: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
  },
  content: { padding: 20, paddingBottom: 48 },
  headerRow: { marginBottom: 8 },
  navBack: { fontSize: 16, fontWeight: '600' },
  notFoundEmoji: { fontSize: 48, marginBottom: 12 },
  notFoundText: { fontSize: 16, marginBottom: 20, textAlign: 'center' },
  backBtn: {
    paddingHorizontal: 24,
    paddingVertical: 12,
    borderRadius: 12,
  },
  backBtnText: { fontWeight: '700' },
  titleBlock: { alignItems: 'center', marginBottom: 20 },
  emoji: { fontSize: 56 },
  name: { fontSize: 24, fontWeight: '800', marginTop: 8, textAlign: 'center' },
  badgeRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginTop: 10,
  },
  frequency: { fontSize: 13, marginTop: 8 },
  card: {
    borderRadius: 16,
    borderWidth: StyleSheet.hairlineWidth,
    padding: 16,
    marginBottom: 16,
  },
  statsRow: { flexDirection: 'row', gap: 12, marginBottom: 16 },
  statCard: {
    flex: 1,
    borderRadius: 16,
    borderWidth: StyleSheet.hairlineWidth,
    padding: 16,
    alignItems: 'center',
  },
  statValue: { fontSize: 22, fontWeight: '800' },
  statLabel: { fontSize: 12, marginTop: 4, textAlign: 'center' },
  aiSection: { marginBottom: 16 },
  aiButton: {
    borderRadius: 14,
    paddingVertical: 14,
    alignItems: 'center',
    marginBottom: 12,
  },
  aiButtonText: { fontSize: 16, fontWeight: '700' },
  aiResponse: { fontSize: 15, lineHeight: 22 },
  upgradeNote: {
    fontSize: 14,
    textAlign: 'center',
    marginBottom: 16,
  },
  actions: { gap: 12 },
  actionBtn: {
    borderRadius: 14,
    borderWidth: 1,
    paddingVertical: 14,
    alignItems: 'center',
  },
  actionText: { fontSize: 16, fontWeight: '700' },
});
