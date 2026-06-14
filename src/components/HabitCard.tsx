import React from 'react';
import { StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { Habit, frequencyLabel } from '../types';
import { getCategoryMeta } from '../constants/categories';
import { useTheme } from '../hooks/useTheme';
import CategoryBadge from './CategoryBadge';
import StreakBadge from './StreakBadge';

interface HabitCardProps {
  habit: Habit;
  completed: boolean;
  streak: number;
  onToggle: () => void;
  onPress?: () => void;
}

export default function HabitCard({
  habit,
  completed,
  streak,
  onToggle,
  onPress,
}: HabitCardProps) {
  const theme = useTheme();
  const meta = getCategoryMeta(habit.category);

  return (
    <TouchableOpacity
      activeOpacity={0.85}
      onPress={onPress}
      style={[
        styles.card,
        {
          backgroundColor: theme.card,
          shadowColor: theme.shadow,
          borderColor: theme.border,
        },
      ]}
    >
      <View style={[styles.iconCircle, { backgroundColor: meta.color + '22' }]}>
        <Text style={styles.emoji}>{habit.emoji}</Text>
      </View>

      <View style={styles.info}>
        <Text style={[styles.name, { color: theme.text }]} numberOfLines={1}>
          {habit.name}
        </Text>
        <View style={styles.metaRow}>
          <CategoryBadge category={habit.category} small />
          <Text style={[styles.frequency, { color: theme.textSecondary }]}>
            {frequencyLabel(habit.frequency)}
          </Text>
        </View>
        <View style={styles.streakRow}>
          <StreakBadge streak={streak} size="sm" />
        </View>
      </View>

      <TouchableOpacity
        activeOpacity={0.7}
        onPress={onToggle}
        hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
        style={[
          styles.checkbox,
          completed
            ? { backgroundColor: theme.success, borderColor: theme.success }
            : { borderColor: theme.border },
        ]}
      >
        {completed && <Text style={styles.checkmark}>✓</Text>}
      </TouchableOpacity>
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  card: {
    flexDirection: 'row',
    alignItems: 'center',
    borderRadius: 16,
    padding: 14,
    marginVertical: 6,
    borderWidth: StyleSheet.hairlineWidth,
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.08,
    shadowRadius: 6,
    elevation: 2,
  },
  iconCircle: {
    width: 48,
    height: 48,
    borderRadius: 24,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 12,
  },
  emoji: {
    fontSize: 24,
  },
  info: {
    flex: 1,
  },
  name: {
    fontSize: 16,
    fontWeight: '700',
    marginBottom: 4,
  },
  metaRow: {
    flexDirection: 'row',
    alignItems: 'center',
    flexWrap: 'wrap',
  },
  frequency: {
    fontSize: 12,
    marginLeft: 8,
  },
  streakRow: {
    marginTop: 6,
  },
  checkbox: {
    width: 40,
    height: 40,
    borderRadius: 20,
    borderWidth: 2,
    alignItems: 'center',
    justifyContent: 'center',
    marginLeft: 12,
  },
  checkmark: {
    color: '#FFFFFF',
    fontSize: 22,
    fontWeight: '900',
  },
});
