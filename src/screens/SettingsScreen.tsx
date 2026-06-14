import React, { useEffect, useState } from 'react';
import {
  ActivityIndicator,
  Alert,
  ScrollView,
  StyleSheet,
  Switch,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { BottomTabScreenProps } from '@react-navigation/bottom-tabs';
import { MainTabsParamList } from '../navigation/types';
import { useTheme, useThemeControls } from '../hooks/useTheme';
import { useSubscription } from '../hooks/useSubscription';
import { getSettings, updateSettings } from '../storage/settings';
import {
  cancelAllReminders,
  requestPermissions,
  scheduleStruggleCheck,
} from '../services/notifications';
import { UserSettings } from '../types';
import PaywallModal from '../components/PaywallModal';

type Props = BottomTabScreenProps<MainTabsParamList, 'Settings'>;

type DarkOption = { key: 'system' | 'light' | 'dark'; label: string };
const DARK_OPTIONS: DarkOption[] = [
  { key: 'system', label: 'System' },
  { key: 'light', label: 'Light' },
  { key: 'dark', label: 'Dark' },
];

export default function SettingsScreen(_props: Props) {
  const theme = useTheme();
  const { override, setOverride } = useThemeControls();
  const subscription = useSubscription();

  const [settings, setSettings] = useState<UserSettings | null>(null);
  const [name, setName] = useState('');
  const [savingName, setSavingName] = useState(false);
  const [paywallVisible, setPaywallVisible] = useState(false);

  useEffect(() => {
    (async () => {
      const s = await getSettings();
      setSettings(s);
      setName(s.name);
    })();
  }, []);

  const handleSaveName = async () => {
    if (!settings) return;
    setSavingName(true);
    try {
      const next = await updateSettings({ name: name.trim() });
      setSettings(next);
    } finally {
      setSavingName(false);
    }
  };

  const handleToggleNotifications = async (value: boolean) => {
    if (!settings) return;
    const next = await updateSettings({ notificationsEnabled: value });
    setSettings(next);
    if (value) {
      await requestPermissions().catch(() => {});
      await scheduleStruggleCheck().catch(() => {});
    } else {
      await cancelAllReminders().catch(() => {});
    }
  };

  const handleDarkMode = async (key: DarkOption['key']) => {
    const value = key === 'system' ? null : key;
    setOverride(value);
    const next = await updateSettings({ darkModeOverride: value });
    setSettings(next);
  };

  const handleCancelSubscription = () => {
    Alert.alert(
      'Manage subscription',
      'Cancel your Premium subscription? You will lose access to AI coaching and unlimited habits.',
      [
        { text: 'Keep Premium', style: 'cancel' },
        {
          text: 'Cancel subscription',
          style: 'destructive',
          onPress: async () => {
            await subscription.cancel();
          },
        },
      ]
    );
  };

  const currentDark: DarkOption['key'] =
    override === null ? 'system' : override;

  if (!settings) {
    return (
      <SafeAreaView
        style={[styles.center, { backgroundColor: theme.background }]}
      >
        <ActivityIndicator color={theme.primary} />
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView
      style={[styles.flex, { backgroundColor: theme.background }]}
      edges={['top']}
    >
      <ScrollView contentContainerStyle={styles.content}>
        <Text style={[styles.screenTitle, { color: theme.text }]}>Settings</Text>

        {/* Profile */}
        <Section title="Profile" theme={theme}>
          <Text style={[styles.label, { color: theme.textSecondary }]}>
            Your name
          </Text>
          <View style={styles.nameRow}>
            <TextInput
              style={[
                styles.input,
                {
                  backgroundColor: theme.cardElevated,
                  color: theme.text,
                  borderColor: theme.border,
                },
              ]}
              value={name}
              onChangeText={setName}
              placeholder="Enter your name"
              placeholderTextColor={theme.textSecondary}
            />
            <TouchableOpacity
              style={[styles.saveButton, { backgroundColor: theme.primary }]}
              onPress={handleSaveName}
              disabled={savingName || name.trim() === settings.name}
            >
              {savingName ? (
                <ActivityIndicator color={theme.white} size="small" />
              ) : (
                <Text style={[styles.saveText, { color: theme.white }]}>
                  Save
                </Text>
              )}
            </TouchableOpacity>
          </View>
        </Section>

        {/* Notifications */}
        <Section title="Notifications" theme={theme}>
          <View style={styles.rowBetween}>
            <View style={styles.rowText}>
              <Text style={[styles.rowTitle, { color: theme.text }]}>
                Reminders & nudges
              </Text>
              <Text style={[styles.rowSub, { color: theme.textSecondary }]}>
                Daily check-ins to keep your streaks alive.
              </Text>
            </View>
            <Switch
              value={!!settings.notificationsEnabled}
              onValueChange={handleToggleNotifications}
              trackColor={{ false: theme.border, true: theme.primary }}
            />
          </View>
        </Section>

        {/* Premium */}
        <Section title="Premium" theme={theme}>
          <View style={styles.rowBetween}>
            <Text style={[styles.rowTitle, { color: theme.text }]}>Status</Text>
            <Text
              style={[
                styles.status,
                { color: subscription.isPremium ? theme.streak : theme.textSecondary },
              ]}
            >
              {subscription.loading
                ? '…'
                : subscription.isPremium
                  ? 'Premium ⭐'
                  : 'Free'}
            </Text>
          </View>
          {subscription.isPremium ? (
            <TouchableOpacity
              style={[
                styles.outlineButton,
                { borderColor: theme.danger },
              ]}
              onPress={handleCancelSubscription}
            >
              <Text style={[styles.outlineText, { color: theme.danger }]}>
                Manage / cancel subscription
              </Text>
            </TouchableOpacity>
          ) : (
            <TouchableOpacity
              style={[styles.filledButton, { backgroundColor: theme.primary }]}
              onPress={() => setPaywallVisible(true)}
            >
              <Text style={[styles.filledText, { color: theme.white }]}>
                Upgrade to Premium
              </Text>
            </TouchableOpacity>
          )}
        </Section>

        {/* Appearance */}
        <Section title="Appearance" theme={theme}>
          <Text style={[styles.label, { color: theme.textSecondary }]}>
            Dark mode
          </Text>
          <View
            style={[
              styles.segment,
              { backgroundColor: theme.cardElevated, borderColor: theme.border },
            ]}
          >
            {DARK_OPTIONS.map((opt) => {
              const active = currentDark === opt.key;
              return (
                <TouchableOpacity
                  key={opt.key}
                  style={[
                    styles.segmentButton,
                    active && { backgroundColor: theme.primary },
                  ]}
                  onPress={() => handleDarkMode(opt.key)}
                >
                  <Text
                    style={[
                      styles.segmentText,
                      { color: active ? theme.white : theme.text },
                    ]}
                  >
                    {opt.label}
                  </Text>
                </TouchableOpacity>
              );
            })}
          </View>
        </Section>

        {/* About */}
        <Section title="About" theme={theme}>
          <Text style={[styles.aboutName, { color: theme.text }]}>
            HabitCoach
          </Text>
          <Text style={[styles.aboutVersion, { color: theme.textSecondary }]}>
            Version 1.0.0
          </Text>
          <Text style={[styles.aboutTagline, { color: theme.textSecondary }]}>
            Small steps, every day. You've got this 💪
          </Text>
        </Section>
      </ScrollView>

      <PaywallModal
        visible={paywallVisible}
        onClose={() => setPaywallVisible(false)}
        onPurchase={async () => {
          await subscription.purchase();
          setPaywallVisible(false);
        }}
        onRestore={async () => {
          await subscription.restore();
          setPaywallVisible(false);
        }}
      />
    </SafeAreaView>
  );
}

function Section({
  title,
  theme,
  children,
}: {
  title: string;
  theme: ReturnType<typeof useTheme>;
  children: React.ReactNode;
}) {
  return (
    <View style={styles.section}>
      <Text style={[styles.sectionTitle, { color: theme.textSecondary }]}>
        {title.toUpperCase()}
      </Text>
      <View
        style={[
          styles.card,
          { backgroundColor: theme.card, borderColor: theme.border },
        ]}
      >
        {children}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  center: { flex: 1, alignItems: 'center', justifyContent: 'center' },
  content: { padding: 20, paddingBottom: 48 },
  screenTitle: { fontSize: 28, fontWeight: '800', marginBottom: 16 },
  section: { marginBottom: 20 },
  sectionTitle: {
    fontSize: 12,
    fontWeight: '700',
    letterSpacing: 0.5,
    marginBottom: 8,
    marginLeft: 4,
  },
  card: {
    borderRadius: 16,
    borderWidth: StyleSheet.hairlineWidth,
    padding: 16,
  },
  label: { fontSize: 13, marginBottom: 8 },
  nameRow: { flexDirection: 'row', gap: 10 },
  input: {
    flex: 1,
    borderRadius: 12,
    borderWidth: StyleSheet.hairlineWidth,
    paddingHorizontal: 14,
    paddingVertical: 10,
    fontSize: 15,
  },
  saveButton: {
    borderRadius: 12,
    paddingHorizontal: 18,
    justifyContent: 'center',
    alignItems: 'center',
  },
  saveText: { fontWeight: '700' },
  rowBetween: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  rowText: { flex: 1, paddingRight: 12 },
  rowTitle: { fontSize: 15, fontWeight: '600' },
  rowSub: { fontSize: 13, marginTop: 2 },
  status: { fontSize: 15, fontWeight: '700' },
  filledButton: {
    borderRadius: 12,
    paddingVertical: 14,
    alignItems: 'center',
    marginTop: 14,
  },
  filledText: { fontSize: 16, fontWeight: '700' },
  outlineButton: {
    borderRadius: 12,
    borderWidth: 1,
    paddingVertical: 14,
    alignItems: 'center',
    marginTop: 14,
  },
  outlineText: { fontSize: 15, fontWeight: '700' },
  segment: {
    flexDirection: 'row',
    borderRadius: 12,
    borderWidth: StyleSheet.hairlineWidth,
    padding: 4,
    gap: 4,
  },
  segmentButton: {
    flex: 1,
    paddingVertical: 10,
    borderRadius: 9,
    alignItems: 'center',
  },
  segmentText: { fontSize: 14, fontWeight: '600' },
  aboutName: { fontSize: 18, fontWeight: '800' },
  aboutVersion: { fontSize: 13, marginTop: 4 },
  aboutTagline: { fontSize: 14, marginTop: 10, lineHeight: 20 },
});
