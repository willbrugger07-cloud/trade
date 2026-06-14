import React, { useCallback, useEffect, useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  ActivityIndicator,
  RefreshControl,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useFocusEffect } from '@react-navigation/native';
import { BottomTabScreenProps } from '@react-navigation/bottom-tabs';
import { useTheme } from '../hooks/useTheme';
import { useHabits } from '../hooks/useHabits';
import { useSubscription } from '../hooks/useSubscription';
import { getSettings } from '../storage/settings';
import { formatLongDate, greetingForHour } from '../utils/date';
import HabitCard from '../components/HabitCard';
import StreakBadge from '../components/StreakBadge';
import CompletionAnimation from '../components/CompletionAnimation';
import { PALETTE } from '../constants/colors';
import { MainTabsParamList, RootStackParamList } from '../navigation/types';

type Props = BottomTabScreenProps<MainTabsParamList, 'Dashboard'>;

export default function DashboardScreen({ navigation }: Props) {
  const theme = useTheme();
  const {
    habits,
    completions,
    loading,
    refresh,
    toggleComplete,
    isCompleted,
    getStreakForHabit,
    getTodayCompletions,
  } = useHabits();
  const { isPremium, freeHabitLimit } = useSubscription();
  const [name, setName] = useState('Friend');
  const [showAnim, setShowAnim] = useState(false);

  useEffect(() => {
    getSettings().then((s) => setName(s.name || 'Friend'));
  }, []);

  useFocusEffect(
    useCallback(() => {
      refresh();
    }, [refresh])
  );

  const rootNav =
    navigation.getParent<
      import('@react-navigation/stack').StackNavigationProp<RootStackParamList>
    >();

  const onToggle = async (habitId: string) => {
    const wasDone = isCompleted(habitId);
    await toggleComplete(habitId);
    if (!wasDone) {
      setShowAnim(true);
    }
  };

  const onAddPress = () => {
    if (!isPremium && habits.length >= freeHabitLimit) {
      rootNav?.navigate('Paywall');
    } else {
      rootNav?.navigate('AddHabit');
    }
  };

  const todayDone = getTodayCompletions().length;
  const total = habits.length;
  const rate = total > 0 ? Math.round((todayDone / total) * 100) : 0;
  const topStreaks = [...habits]
    .map((h) => ({ habit: h, streak: getStreakForHabit(h.id) }))
    .sort((a, b) => b.streak - a.streak)
    .slice(0, 5)
    .filter((s) => s.streak > 0);

  if (loading) {
    return (
      <View style={[styles.center, { backgroundColor: theme.background }]}>
        <ActivityIndicator color={theme.primary} size="large" />
      </View>
    );
  }

  return (
    <View style={[styles.root, { backgroundColor: theme.background }]}>
      <SafeAreaView edges={['top']} style={{ flex: 1 }}>
        <ScrollView
          contentContainerStyle={styles.content}
          refreshControl={
            <RefreshControl
              refreshing={false}
              onRefresh={refresh}
              tintColor={theme.primary}
            />
          }
        >
          <Text style={[styles.greeting, { color: theme.text }]}>
            {greetingForHour(new Date().getHours())}, {name}!
          </Text>
          <Text style={[styles.date, { color: theme.textSecondary }]}>
            {formatLongDate(new Date())}
          </Text>

          <View
            style={[
              styles.progressCard,
              { backgroundColor: theme.card, shadowColor: theme.shadow },
            ]}
          >
            <View style={styles.ring}>
              <View
                style={[
                  styles.ringInner,
                  { borderColor: theme.primary, backgroundColor: theme.primaryMuted },
                ]}
              >
                <Text style={[styles.ringPct, { color: theme.primary }]}>
                  {rate}%
                </Text>
              </View>
            </View>
            <View style={{ flex: 1, marginLeft: 16 }}>
              <Text style={[styles.progressTitle, { color: theme.text }]}>
                {todayDone}/{total} done today
              </Text>
              <Text style={[styles.progressSub, { color: theme.textSecondary }]}>
                {rate === 100 && total > 0
                  ? 'Perfect day! Amazing work. 🎉'
                  : total === 0
                  ? 'Add your first habit to begin.'
                  : 'Keep going — every check-in counts.'}
              </Text>
            </View>
          </View>

          {topStreaks.length > 0 && (
            <View style={styles.section}>
              <Text style={[styles.sectionTitle, { color: theme.text }]}>
                🔥 Streaks
              </Text>
              <ScrollView
                horizontal
                showsHorizontalScrollIndicator={false}
                contentContainerStyle={styles.streakRow}
              >
                {topStreaks.map(({ habit, streak }) => (
                  <View
                    key={habit.id}
                    style={[
                      styles.streakCard,
                      { backgroundColor: theme.card, shadowColor: theme.shadow },
                    ]}
                  >
                    <Text style={styles.streakEmoji}>{habit.emoji}</Text>
                    <StreakBadge streak={streak} size="sm" />
                    <Text
                      numberOfLines={1}
                      style={[styles.streakName, { color: theme.textSecondary }]}
                    >
                      {habit.name}
                    </Text>
                  </View>
                ))}
              </ScrollView>
            </View>
          )}

          <View style={styles.section}>
            <Text style={[styles.sectionTitle, { color: theme.text }]}>
              Today's habits
            </Text>
            {habits.length === 0 ? (
              <View
                style={[
                  styles.empty,
                  { backgroundColor: theme.card, borderColor: theme.border },
                ]}
              >
                <Text style={styles.emptyEmoji}>🌱</Text>
                <Text style={[styles.emptyTitle, { color: theme.text }]}>
                  No habits yet
                </Text>
                <Text
                  style={[styles.emptySub, { color: theme.textSecondary }]}
                >
                  Tap the + button to create your first habit.
                </Text>
              </View>
            ) : (
              habits.map((habit) => (
                <HabitCard
                  key={habit.id}
                  habit={habit}
                  completed={isCompleted(habit.id)}
                  streak={getStreakForHabit(habit.id)}
                  onToggle={() => onToggle(habit.id)}
                  onPress={() =>
                    rootNav?.navigate('HabitDetail', { habitId: habit.id })
                  }
                />
              ))
            )}
          </View>
          <View style={{ height: 96 }} />
        </ScrollView>
      </SafeAreaView>

      <TouchableOpacity
        style={[styles.fab, { backgroundColor: theme.primary }]}
        onPress={onAddPress}
        activeOpacity={0.85}
      >
        <Text style={styles.fabText}>＋</Text>
      </TouchableOpacity>

      <CompletionAnimation
        visible={showAnim}
        onDone={() => setShowAnim(false)}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1 },
  center: { flex: 1, alignItems: 'center', justifyContent: 'center' },
  content: { padding: 20 },
  greeting: { fontSize: 26, fontWeight: '800' },
  date: { fontSize: 15, marginTop: 4, marginBottom: 20 },
  progressCard: {
    flexDirection: 'row',
    alignItems: 'center',
    borderRadius: 18,
    padding: 18,
    shadowOpacity: 0.06,
    shadowRadius: 10,
    shadowOffset: { width: 0, height: 4 },
    elevation: 2,
  },
  ring: { width: 72, height: 72 },
  ringInner: {
    flex: 1,
    borderRadius: 36,
    borderWidth: 5,
    alignItems: 'center',
    justifyContent: 'center',
  },
  ringPct: { fontSize: 18, fontWeight: '800' },
  progressTitle: { fontSize: 18, fontWeight: '700' },
  progressSub: { fontSize: 14, marginTop: 4, lineHeight: 19 },
  section: { marginTop: 26 },
  sectionTitle: { fontSize: 18, fontWeight: '700', marginBottom: 12 },
  streakRow: { gap: 12, paddingRight: 8 },
  streakCard: {
    width: 96,
    borderRadius: 16,
    padding: 12,
    alignItems: 'center',
    shadowOpacity: 0.06,
    shadowRadius: 8,
    shadowOffset: { width: 0, height: 3 },
    elevation: 2,
  },
  streakEmoji: { fontSize: 28, marginBottom: 6 },
  streakName: { fontSize: 12, marginTop: 6, maxWidth: 80 },
  empty: {
    borderRadius: 16,
    borderWidth: 1,
    padding: 28,
    alignItems: 'center',
  },
  emptyEmoji: { fontSize: 40, marginBottom: 10 },
  emptyTitle: { fontSize: 17, fontWeight: '700' },
  emptySub: { fontSize: 14, textAlign: 'center', marginTop: 6 },
  fab: {
    position: 'absolute',
    right: 24,
    bottom: 28,
    width: 60,
    height: 60,
    borderRadius: 30,
    alignItems: 'center',
    justifyContent: 'center',
    shadowColor: PALETTE.primary,
    shadowOpacity: 0.4,
    shadowRadius: 10,
    shadowOffset: { width: 0, height: 6 },
    elevation: 6,
  },
  fabText: { color: PALETTE.white, fontSize: 32, fontWeight: '600', marginTop: -2 },
});
