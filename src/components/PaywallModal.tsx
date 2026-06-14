import React, { useState } from 'react';
import {
  ActivityIndicator,
  Modal,
  ScrollView,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { useTheme } from '../hooks/useTheme';
import { PRICING, PREMIUM_FEATURES } from '../constants/config';

type Plan = 'monthly' | 'yearly';

interface PaywallModalProps {
  visible: boolean;
  onClose: () => void;
  onPurchase: (plan: Plan) => Promise<void> | void;
  onRestore: () => Promise<void> | void;
}

export default function PaywallModal({
  visible,
  onClose,
  onPurchase,
  onRestore,
}: PaywallModalProps) {
  const theme = useTheme();
  const [selected, setSelected] = useState<Plan>('yearly');
  const [purchasing, setPurchasing] = useState(false);

  const handlePurchase = async () => {
    if (purchasing) return;
    setPurchasing(true);
    try {
      await onPurchase(selected);
    } finally {
      setPurchasing(false);
    }
  };

  return (
    <Modal
      visible={visible}
      animationType="slide"
      transparent
      onRequestClose={onClose}
    >
      <View style={styles.backdrop}>
        <View style={[styles.card, { backgroundColor: theme.card }]}>
          <LinearGradient
            colors={[theme.primary, '#8B82FF']}
            start={{ x: 0, y: 0 }}
            end={{ x: 1, y: 1 }}
            style={styles.header}
          >
            <TouchableOpacity
              onPress={onClose}
              hitSlop={{ top: 12, bottom: 12, left: 12, right: 12 }}
              style={styles.closeButton}
            >
              <Text style={styles.closeText}>✕</Text>
            </TouchableOpacity>
            <Text style={styles.headerIcon}>⭐</Text>
            <Text style={styles.headerTitle}>Unlock Premium</Text>
          </LinearGradient>

          <ScrollView
            contentContainerStyle={styles.body}
            showsVerticalScrollIndicator={false}
          >
            <View style={styles.features}>
              {PREMIUM_FEATURES.map((feature) => (
                <View key={feature} style={styles.featureRow}>
                  <Text style={[styles.featureCheck, { color: theme.success }]}>
                    ✓
                  </Text>
                  <Text style={[styles.featureText, { color: theme.text }]}>
                    {feature}
                  </Text>
                </View>
              ))}
            </View>

            <PlanOption
              label="Monthly"
              price={PRICING.monthly}
              selected={selected === 'monthly'}
              onPress={() => setSelected('monthly')}
            />
            <PlanOption
              label="Yearly"
              price={PRICING.yearly}
              badge="Best value"
              selected={selected === 'yearly'}
              onPress={() => setSelected('yearly')}
            />

            <TouchableOpacity
              activeOpacity={0.85}
              onPress={handlePurchase}
              disabled={purchasing}
              style={[styles.purchaseButton, { backgroundColor: theme.primary }]}
            >
              {purchasing ? (
                <ActivityIndicator color="#FFFFFF" />
              ) : (
                <Text style={styles.purchaseText}>Unlock Premium</Text>
              )}
            </TouchableOpacity>

            <TouchableOpacity
              onPress={() => onRestore()}
              style={styles.restoreButton}
            >
              <Text style={[styles.restoreText, { color: theme.textSecondary }]}>
                Restore purchases
              </Text>
            </TouchableOpacity>
          </ScrollView>
        </View>
      </View>
    </Modal>
  );
}

interface PlanOptionProps {
  label: string;
  price: string;
  badge?: string;
  selected: boolean;
  onPress: () => void;
}

function PlanOption({
  label,
  price,
  badge,
  selected,
  onPress,
}: PlanOptionProps) {
  const theme = useTheme();
  return (
    <TouchableOpacity
      activeOpacity={0.85}
      onPress={onPress}
      style={[
        styles.plan,
        {
          backgroundColor: theme.cardElevated,
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
      {badge && (
        <View style={[styles.badge, { backgroundColor: theme.success }]}>
          <Text style={styles.badgeText}>{badge}</Text>
        </View>
      )}
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  backdrop: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.5)',
    justifyContent: 'flex-end',
  },
  card: {
    borderTopLeftRadius: 24,
    borderTopRightRadius: 24,
    maxHeight: '90%',
    overflow: 'hidden',
  },
  header: {
    paddingTop: 28,
    paddingBottom: 24,
    alignItems: 'center',
  },
  closeButton: {
    position: 'absolute',
    top: 14,
    right: 16,
    zIndex: 1,
  },
  closeText: {
    color: '#FFFFFF',
    fontSize: 20,
    fontWeight: '700',
  },
  headerIcon: {
    fontSize: 40,
    marginBottom: 6,
  },
  headerTitle: {
    color: '#FFFFFF',
    fontSize: 24,
    fontWeight: '800',
  },
  body: {
    padding: 20,
  },
  features: {
    marginBottom: 20,
  },
  featureRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 10,
  },
  featureCheck: {
    fontSize: 16,
    fontWeight: '900',
    marginRight: 10,
  },
  featureText: {
    fontSize: 15,
    flex: 1,
  },
  plan: {
    flexDirection: 'row',
    alignItems: 'center',
    borderWidth: 2,
    borderRadius: 14,
    padding: 14,
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
  radioDot: {
    width: 12,
    height: 12,
    borderRadius: 6,
  },
  planInfo: {
    flex: 1,
  },
  planLabel: {
    fontSize: 16,
    fontWeight: '700',
  },
  planPrice: {
    fontSize: 14,
    marginTop: 2,
  },
  badge: {
    borderRadius: 999,
    paddingHorizontal: 10,
    paddingVertical: 4,
  },
  badgeText: {
    color: '#FFFFFF',
    fontSize: 11,
    fontWeight: '700',
  },
  purchaseButton: {
    borderRadius: 14,
    paddingVertical: 16,
    alignItems: 'center',
    justifyContent: 'center',
    marginTop: 8,
  },
  purchaseText: {
    color: '#FFFFFF',
    fontSize: 17,
    fontWeight: '800',
  },
  restoreButton: {
    alignItems: 'center',
    paddingVertical: 14,
  },
  restoreText: {
    fontSize: 14,
    fontWeight: '600',
  },
});
