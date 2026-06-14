import React, { useState } from 'react';
import {
  ActivityIndicator,
  ScrollView,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { LinearGradient } from 'expo-linear-gradient';
import { StackScreenProps } from '@react-navigation/stack';
import { RootStackParamList } from '../navigation/types';
import { useTheme } from '../hooks/useTheme';
import { useSubscription } from '../hooks/useSubscription';
import { PREMIUM_FEATURES, PRICING } from '../constants/config';

type Props = StackScreenProps<RootStackParamList, 'Paywall'>;
type Plan = 'monthly' | 'yearly';

// Free tier includes the first couple of features only.
const FREE_INCLUDED = 2;

export default function PaywallScreen({ navigation }: Props) {
  const theme = useTheme();
  const subscription = useSubscription();

  const [selected, setSelected] = useState<Plan>('yearly');
  const [purchasing, setPurchasing] = useState(false);
  const [restoring, setRestoring] = useState(false);

  const handlePurchase = async () => {
    if (purchasing) return;
    setPurchasing(true);
    try {
      await subscription.purchase();
      navigation.goBack();
    } finally {
      setPurchasing(false);
    }
  };

  const handleRestore = async () => {
    if (restoring) return;
    setRestoring(true);
    try {
      await subscription.restore();
    } finally {
      setRestoring(false);
    }
  };

  return (
    <SafeAreaView
      style={[styles.flex, { backgroundColor: theme.background }]}
      edges={['bottom']}
    >
      <ScrollView contentContainerStyle={styles.content} bounces={false}>
        <LinearGradient
          colors={[theme.primary, '#8B82FF']}
          start={{ x: 0, y: 0 }}
          end={{ x: 1, y: 1 }}
          style={styles.header}
        >
          <TouchableOpacity
            onPress={() => navigation.goBack()}
            hitSlop={{ top: 12, bottom: 12, left: 12, right: 12 }}
            style={styles.closeButton}
          >
            <Text style={styles.closeText}>✕</Text>
          </TouchableOpacity>
          <Text style={styles.star}>⭐</Text>
          <Text style={styles.headerTitle}>Unlock Premium</Text>
          <Text style={styles.headerSub}>
            Everything you need to build lasting habits.
          </Text>
        </LinearGradient>

        <View style={styles.body}>
          {/* Feature comparison */}
          <View
            style={[
              styles.table,
              { backgroundColor: theme.card, borderColor: theme.border },
            ]}
          >
            <View style={[styles.tableRow, styles.tableHeaderRow]}>
              <Text style={[styles.featureCol, styles.tableHeaderText, { color: theme.text }]}>
                Feature
              </Text>
              <Text style={[styles.checkCol, styles.tableHeaderText, { color: theme.textSecondary }]}>
                Free
              </Text>
              <Text style={[styles.checkCol, styles.tableHeaderText, { color: theme.primary }]}>
                Premium
              </Text>
            </View>
            {PREMIUM_FEATURES.map((feature, i) => {
              const inFree = i < FREE_INCLUDED;
              return (
                <View
                  key={feature}
                  style={[styles.tableRow, { borderTopColor: theme.border }]}
                >
                  <Text style={[styles.featureCol, { color: theme.text }]}>
                    {feature}
                  </Text>
                  <Text
                    style={[
                      styles.checkCol,
                      styles.mark,
                      { color: inFree ? theme.success : theme.textSecondary },
                    ]}
                  >
                    {inFree ? '✓' : '✕'}
                  </Text>
                  <Text
                    style={[styles.checkCol, styles.mark, { color: theme.success }]}
                  >
                    ✓
                  </Text>
                </View>
              );
            })}
          </View>

          {/* Pricing options */}
          <PlanCard
            label="Monthly"
            price={PRICING.monthly}
            selected={selected === 'monthly'}
            onPress={() => setSelected('monthly')}
            theme={theme}
          />
          <PlanCard
            label="Yearly"
            price={PRICING.yearly}
            note="Best value · save 40%"
            selected={selected === 'yearly'}
            onPress={() => setSelected('yearly')}
            theme={theme}
          />

          <TouchableOpacity
            style={[styles.purchaseButton, { backgroundColor: theme.primary }]}
            onPress={handlePurchase}
            disabled={purchasing}
            activeOpacity={0.85}
          >
            {purchasing ? (
              <ActivityIndicator color={theme.white} />
            ) : (
              <Text style={[styles.purchaseText, { color: theme.white }]}>
                Unlock Premium
              </Text>
            )}
          </TouchableOpacity>

          <TouchableOpacity
            style={styles.restoreButton}
            onPress={handleRestore}
            disabled={restoring}
          >
            <Text style={[styles.restoreText, { color: theme.textSecondary }]}>
              {restoring ? 'Restoring…' : 'Restore purchases'}
            </Text>
          </TouchableOpacity>

          <Text style={[styles.socialProof, { color: theme.textSecondary }]}>
            Join 10,000+ users building better habits 💪
          </Text>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}

function PlanCard({
  label,
  price,
  note,
  selected,
  onPress,
  theme,
}: {
  label: string;
  price: string;
  note?: string;
  selected: boolean;
  onPress: () => void;
  theme: ReturnType<typeof useTheme>;
}) {
  return (
    <TouchableOpacity
      activeOpacity={0.85}
      onPress={onPress}
      style={[
        styles.plan,
        {
          backgroundColor: theme.card,
          borderColor: selected ? theme.primary : theme.border,
        },
      ]}
    >
      <View
        style={[
          styles.radio,
          { borderColor: selected ? theme.primary : theme.border },
        ]}
      >
        {selected && (
          <View style={[styles.radioDot, { backgroundColor: theme.primary }]} />
        )}
      </View>
      <View style={styles.planInfo}>
        <Text style={[styles.planLabel, { color: theme.text }]}>{label}</Text>
        <Text style={[styles.planPrice, { color: theme.textSecondary }]}>
          {price}
        </Text>
      </View>
      {note && (
        <View style={[styles.planBadge, { backgroundColor: theme.success }]}>
          <Text style={styles.planBadgeText}>{note}</Text>
        </View>
      )}
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  content: { paddingBottom: 40 },
  header: {
    paddingTop: 56,
    paddingBottom: 32,
    alignItems: 'center',
    paddingHorizontal: 20,
  },
  closeButton: { position: 'absolute', top: 16, right: 18, zIndex: 1 },
  closeText: { color: '#FFFFFF', fontSize: 22, fontWeight: '700' },
  star: { fontSize: 52, marginBottom: 8 },
  headerTitle: { color: '#FFFFFF', fontSize: 28, fontWeight: '800' },
  headerSub: {
    color: '#FFFFFF',
    fontSize: 14,
    marginTop: 8,
    textAlign: 'center',
    opacity: 0.9,
  },
  body: { padding: 20 },
  table: {
    borderRadius: 16,
    borderWidth: StyleSheet.hairlineWidth,
    overflow: 'hidden',
    marginBottom: 20,
  },
  tableRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 12,
    paddingHorizontal: 14,
  },
  tableHeaderRow: { paddingVertical: 14 },
  tableHeaderText: { fontWeight: '800', fontSize: 13 },
  featureCol: { flex: 1, fontSize: 14, paddingRight: 8 },
  checkCol: { width: 64, textAlign: 'center', fontSize: 14 },
  mark: { fontWeight: '900', fontSize: 16, borderTopWidth: 0 },
  plan: {
    flexDirection: 'row',
    alignItems: 'center',
    borderWidth: 2,
    borderRadius: 14,
    padding: 16,
    marginBottom: 12,
  },
  radio: {
    width: 22,
    height: 22,
    borderRadius: 11,
    borderWidth: 2,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 12,
  },
  radioDot: { width: 12, height: 12, borderRadius: 6 },
  planInfo: { flex: 1 },
  planLabel: { fontSize: 16, fontWeight: '700' },
  planPrice: { fontSize: 14, marginTop: 2 },
  planBadge: {
    borderRadius: 999,
    paddingHorizontal: 10,
    paddingVertical: 4,
    marginLeft: 8,
  },
  planBadgeText: { color: '#FFFFFF', fontSize: 11, fontWeight: '700' },
  purchaseButton: {
    borderRadius: 14,
    paddingVertical: 16,
    alignItems: 'center',
    justifyContent: 'center',
    marginTop: 8,
  },
  purchaseText: { fontSize: 17, fontWeight: '800' },
  restoreButton: { alignItems: 'center', paddingVertical: 14 },
  restoreText: { fontSize: 14, fontWeight: '600' },
  socialProof: {
    textAlign: 'center',
    fontSize: 13,
    marginTop: 6,
  },
});
