import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { Category } from '../types';
import { getCategoryMeta } from '../constants/categories';

interface CategoryBadgeProps {
  category: Category;
  small?: boolean;
}

export default function CategoryBadge({ category, small }: CategoryBadgeProps) {
  const meta = getCategoryMeta(category);

  return (
    <View
      style={[
        styles.badge,
        small && styles.badgeSmall,
        { backgroundColor: meta.color + '22' },
      ]}
    >
      <Text style={[styles.emoji, small && styles.emojiSmall]}>
        {meta.emoji}
      </Text>
      <Text
        style={[styles.label, small && styles.labelSmall, { color: meta.color }]}
      >
        {meta.label}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  badge: {
    flexDirection: 'row',
    alignItems: 'center',
    alignSelf: 'flex-start',
    borderRadius: 999,
    paddingHorizontal: 10,
    paddingVertical: 4,
  },
  badgeSmall: {
    paddingHorizontal: 8,
    paddingVertical: 2,
  },
  emoji: {
    fontSize: 12,
    marginRight: 4,
  },
  emojiSmall: {
    fontSize: 10,
    marginRight: 3,
  },
  label: {
    fontSize: 12,
    fontWeight: '600',
  },
  labelSmall: {
    fontSize: 10,
  },
});
