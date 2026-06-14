import React, { useMemo } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { Completion } from '../types';
import { useTheme } from '../hooks/useTheme';
import { addDays, toDateString } from '../utils/date';

interface HeatmapCalendarProps {
  completions: Completion[];
}

const WEEKS = 12;
const DAYS = WEEKS * 7; // 84

export default function HeatmapCalendar({ completions }: HeatmapCalendarProps) {
  const theme = useTheme();

  const completedSet = useMemo(
    () => new Set(completions.map((c) => c.date)),
    [completions]
  );

  // Build columns of weeks. Oldest day is 83 days ago; newest is today.
  const columns = useMemo(() => {
    const today = new Date();
    const cells: string[] = [];
    for (let i = DAYS - 1; i >= 0; i--) {
      cells.push(toDateString(addDays(today, -i)));
    }
    const cols: string[][] = [];
    for (let w = 0; w < WEEKS; w++) {
      cols.push(cells.slice(w * 7, w * 7 + 7));
    }
    return cols;
  }, []);

  const emptyColor = theme.isDark ? theme.border + '55' : theme.border + '88';

  return (
    <View style={styles.container}>
      <Text style={[styles.title, { color: theme.text }]}>Last 12 weeks</Text>

      <View style={styles.grid}>
        {columns.map((week, wi) => (
          <View key={wi} style={styles.column}>
            {week.map((date) => {
              const done = completedSet.has(date);
              return (
                <View
                  key={date}
                  style={[
                    styles.cell,
                    {
                      backgroundColor: done ? theme.primary : emptyColor,
                    },
                  ]}
                />
              );
            })}
          </View>
        ))}
      </View>

      <View style={styles.legend}>
        <Text style={[styles.legendText, { color: theme.textSecondary }]}>
          Less
        </Text>
        <View style={[styles.cell, { backgroundColor: emptyColor }]} />
        <View style={[styles.cell, { backgroundColor: theme.primary + '55' }]} />
        <View style={[styles.cell, { backgroundColor: theme.primary + 'AA' }]} />
        <View style={[styles.cell, { backgroundColor: theme.primary }]} />
        <Text style={[styles.legendText, { color: theme.textSecondary }]}>
          More
        </Text>
      </View>
    </View>
  );
}

const CELL = 14;

const styles = StyleSheet.create({
  container: {
    alignSelf: 'flex-start',
  },
  title: {
    fontSize: 16,
    fontWeight: '700',
    marginBottom: 10,
  },
  grid: {
    flexDirection: 'row',
  },
  column: {
    marginRight: 3,
  },
  cell: {
    width: CELL,
    height: CELL,
    borderRadius: 3,
    marginBottom: 3,
  },
  legend: {
    flexDirection: 'row',
    alignItems: 'center',
    marginTop: 10,
  },
  legendText: {
    fontSize: 11,
    marginHorizontal: 6,
  },
});
