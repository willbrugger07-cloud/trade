import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { useTheme } from '../hooks/useTheme';

interface StreakBadgeProps {
  streak: number;
  size?: 'sm' | 'md' | 'lg';
}

const SIZES = {
  sm: { emoji: 12, text: 12, gap: 2 },
  md: { emoji: 16, text: 14, gap: 3 },
  lg: { emoji: 22, text: 20, gap: 4 },
} as const;

export default function StreakBadge({ streak, size = 'md' }: StreakBadgeProps) {
  const theme = useTheme();
  const dims = SIZES[size];
  const isActive = streak > 0;
  const color = isActive ? theme.streak : theme.textSecondary;

  return (
    <View style={styles.container}>
      <Text style={[{ fontSize: dims.emoji }, !isActive && styles.muted]}>
        🔥
      </Text>
      <Text
        style={[
          styles.text,
          { fontSize: dims.text, color, marginLeft: dims.gap },
        ]}
      >
        {streak}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  text: {
    fontWeight: '700',
  },
  muted: {
    opacity: 0.4,
  },
});
