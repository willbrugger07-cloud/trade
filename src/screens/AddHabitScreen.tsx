import React, { useEffect, useState } from 'react';
import {
  View,
  Text,
  TextInput,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  Platform,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import DateTimePicker from '@react-native-community/datetimepicker';
import { StackScreenProps } from '@react-navigation/stack';
import { useTheme } from '../hooks/useTheme';
import { useHabits } from '../hooks/useHabits';
import { EMOJI_OPTIONS, CATEGORIES } from '../constants/categories';
import { PALETTE } from '../constants/colors';
import { Category, Frequency, Habit } from '../types';
import { RootStackParamList } from '../navigation/types';

type Props = StackScreenProps<RootStackParamList, 'AddHabit'>;

type FreqMode = 'daily' | 'weekly' | 'times';

export default function AddHabitScreen({ navigation, route }: Props) {
  const theme = useTheme();
  const { habits, addHabit, editHabit } = useHabits();
  const editingId = route.params?.habitId;
  const editing = habits.find((h) => h.id === editingId);

  const [emoji, setEmoji] = useState('🎯');
  const [name, setName] = useState('');
  const [category, setCategory] = useState<Category>('health');
  const [freqMode, setFreqMode] = useState<FreqMode>('daily');
  const [timesPerWeek, setTimesPerWeek] = useState(3);
  const [reminderEnabled, setReminderEnabled] = useState(true);
  const [reminderTime, setReminderTime] = useState('09:00');
  const [showPicker, setShowPicker] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (editing) {
      setEmoji(editing.emoji);
      setName(editing.name);
      setCategory(editing.category);
      if (editing.frequency === 'daily') setFreqMode('daily');
      else if (editing.frequency === 'weekly') setFreqMode('weekly');
      else {
        setFreqMode('times');
        setTimesPerWeek(editing.frequency.timesPerWeek);
      }
      if (editing.reminderTime) {
        setReminderEnabled(true);
        setReminderTime(editing.reminderTime);
      } else {
        setReminderEnabled(false);
      }
      navigation.setOptions({ title: 'Edit Habit' });
    }
  }, [editing, navigation]);

  const buildFrequency = (): Frequency => {
    if (freqMode === 'daily') return 'daily';
    if (freqMode === 'weekly') return 'weekly';
    return { timesPerWeek };
  };

  const onSave = async () => {
    if (!name.trim()) return;
    setSaving(true);
    try {
      const reminder = reminderEnabled ? reminderTime : undefined;
      if (editing) {
        const updated: Habit = {
          ...editing,
          name: name.trim(),
          emoji,
          category,
          frequency: buildFrequency(),
          reminderTime: reminder,
        };
        await editHabit(updated);
      } else {
        await addHabit({
          name: name.trim(),
          emoji,
          category,
          frequency: buildFrequency(),
          reminderTime: reminder,
        });
      }
      navigation.goBack();
    } finally {
      setSaving(false);
    }
  };

  const onPickTime = (_: unknown, date?: Date) => {
    setShowPicker(Platform.OS === 'ios');
    if (date) {
      const h = String(date.getHours()).padStart(2, '0');
      const m = String(date.getMinutes()).padStart(2, '0');
      setReminderTime(`${h}:${m}`);
    }
  };

  const timeAsDate = () => {
    const [h, m] = reminderTime.split(':').map(Number);
    const d = new Date();
    d.setHours(h, m, 0, 0);
    return d;
  };

  return (
    <SafeAreaView
      style={[styles.root, { backgroundColor: theme.background }]}
      edges={['bottom']}
    >
      <ScrollView
        contentContainerStyle={styles.content}
        keyboardShouldPersistTaps="handled"
      >
        <Text style={[styles.label, { color: theme.text }]}>Choose an icon</Text>
        <View style={styles.emojiGrid}>
          {EMOJI_OPTIONS.map((e) => (
            <TouchableOpacity
              key={e}
              onPress={() => setEmoji(e)}
              style={[
                styles.emojiCell,
                {
                  backgroundColor:
                    emoji === e ? theme.primaryMuted : theme.card,
                  borderColor: emoji === e ? theme.primary : theme.border,
                },
              ]}
            >
              <Text style={styles.emojiText}>{e}</Text>
            </TouchableOpacity>
          ))}
        </View>

        <Text style={[styles.label, { color: theme.text }]}>Habit name</Text>
        <TextInput
          value={name}
          onChangeText={setName}
          placeholder="e.g. Morning run"
          placeholderTextColor={theme.textSecondary}
          style={[
            styles.input,
            {
              backgroundColor: theme.card,
              borderColor: theme.border,
              color: theme.text,
            },
          ]}
        />

        <Text style={[styles.label, { color: theme.text }]}>Category</Text>
        <View style={styles.chipRow}>
          {CATEGORIES.map((c) => {
            const active = category === c.key;
            return (
              <TouchableOpacity
                key={c.key}
                onPress={() => setCategory(c.key)}
                style={[
                  styles.chip,
                  {
                    backgroundColor: active ? c.color : c.color + '22',
                    borderColor: c.color,
                  },
                ]}
              >
                <Text
                  style={{
                    color: active ? PALETTE.white : c.color,
                    fontWeight: '600',
                  }}
                >
                  {c.emoji} {c.label}
                </Text>
              </TouchableOpacity>
            );
          })}
        </View>

        <Text style={[styles.label, { color: theme.text }]}>Frequency</Text>
        <View style={styles.segment}>
          {(
            [
              ['daily', 'Daily'],
              ['weekly', 'Weekly'],
              ['times', 'X / week'],
            ] as [FreqMode, string][]
          ).map(([mode, lbl]) => (
            <TouchableOpacity
              key={mode}
              onPress={() => setFreqMode(mode)}
              style={[
                styles.segBtn,
                {
                  backgroundColor:
                    freqMode === mode ? theme.primary : theme.card,
                  borderColor:
                    freqMode === mode ? theme.primary : theme.border,
                },
              ]}
            >
              <Text
                style={{
                  color: freqMode === mode ? PALETTE.white : theme.text,
                  fontWeight: '600',
                }}
              >
                {lbl}
              </Text>
            </TouchableOpacity>
          ))}
        </View>

        {freqMode === 'times' && (
          <View style={styles.stepper}>
            <TouchableOpacity
              onPress={() => setTimesPerWeek((n) => Math.max(1, n - 1))}
              style={[styles.stepBtn, { borderColor: theme.border }]}
            >
              <Text style={[styles.stepSign, { color: theme.text }]}>－</Text>
            </TouchableOpacity>
            <Text style={[styles.stepValue, { color: theme.text }]}>
              {timesPerWeek}x per week
            </Text>
            <TouchableOpacity
              onPress={() => setTimesPerWeek((n) => Math.min(7, n + 1))}
              style={[styles.stepBtn, { borderColor: theme.border }]}
            >
              <Text style={[styles.stepSign, { color: theme.text }]}>＋</Text>
            </TouchableOpacity>
          </View>
        )}

        <View style={styles.reminderHeader}>
          <Text style={[styles.label, { color: theme.text, marginBottom: 0 }]}>
            Daily reminder
          </Text>
          <TouchableOpacity
            onPress={() => setReminderEnabled((v) => !v)}
            style={[
              styles.toggle,
              {
                backgroundColor: reminderEnabled ? theme.primary : theme.border,
              },
            ]}
          >
            <View
              style={[
                styles.knob,
                { alignSelf: reminderEnabled ? 'flex-end' : 'flex-start' },
              ]}
            />
          </TouchableOpacity>
        </View>

        {reminderEnabled && (
          <TouchableOpacity
            onPress={() => setShowPicker(true)}
            style={[
              styles.timeBtn,
              { backgroundColor: theme.card, borderColor: theme.border },
            ]}
          >
            <Text style={{ color: theme.text, fontSize: 16 }}>
              ⏰ Remind me at {reminderTime}
            </Text>
          </TouchableOpacity>
        )}

        {showPicker && (
          <DateTimePicker
            value={timeAsDate()}
            mode="time"
            is24Hour
            display={Platform.OS === 'ios' ? 'spinner' : 'default'}
            onChange={onPickTime}
          />
        )}
      </ScrollView>

      <View style={styles.footer}>
        <TouchableOpacity
          disabled={!name.trim() || saving}
          onPress={onSave}
          style={[
            styles.saveBtn,
            {
              backgroundColor: theme.primary,
              opacity: !name.trim() || saving ? 0.5 : 1,
            },
          ]}
        >
          <Text style={styles.saveText}>
            {saving ? 'Saving…' : editing ? 'Save changes' : 'Create habit'}
          </Text>
        </TouchableOpacity>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1 },
  content: { padding: 20 },
  label: { fontSize: 16, fontWeight: '700', marginBottom: 12, marginTop: 8 },
  emojiGrid: { flexDirection: 'row', flexWrap: 'wrap', gap: 10 },
  emojiCell: {
    width: 52,
    height: 52,
    borderRadius: 12,
    borderWidth: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  emojiText: { fontSize: 24 },
  input: {
    borderWidth: 1,
    borderRadius: 14,
    paddingHorizontal: 16,
    paddingVertical: 14,
    fontSize: 16,
  },
  chipRow: { flexDirection: 'row', flexWrap: 'wrap', gap: 10 },
  chip: {
    paddingHorizontal: 14,
    paddingVertical: 10,
    borderRadius: 20,
    borderWidth: 1,
  },
  segment: { flexDirection: 'row', gap: 10 },
  segBtn: {
    flex: 1,
    paddingVertical: 12,
    borderRadius: 12,
    borderWidth: 1,
    alignItems: 'center',
  },
  stepper: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 20,
    marginTop: 16,
  },
  stepBtn: {
    width: 44,
    height: 44,
    borderRadius: 22,
    borderWidth: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  stepSign: { fontSize: 22, fontWeight: '600' },
  stepValue: { fontSize: 16, fontWeight: '600', minWidth: 120, textAlign: 'center' },
  reminderHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginTop: 20,
    marginBottom: 12,
  },
  toggle: {
    width: 52,
    height: 30,
    borderRadius: 15,
    padding: 3,
    justifyContent: 'center',
  },
  knob: {
    width: 24,
    height: 24,
    borderRadius: 12,
    backgroundColor: PALETTE.white,
  },
  timeBtn: {
    borderWidth: 1,
    borderRadius: 14,
    padding: 16,
  },
  footer: { padding: 20 },
  saveBtn: { paddingVertical: 16, borderRadius: 14, alignItems: 'center' },
  saveText: { color: PALETTE.white, fontSize: 17, fontWeight: '700' },
});
