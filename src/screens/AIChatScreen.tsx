import React, { useCallback, useEffect, useRef, useState } from 'react';
import {
  ActivityIndicator,
  FlatList,
  KeyboardAvoidingView,
  Platform,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { BottomTabScreenProps } from '@react-navigation/bottom-tabs';
import { MainTabsParamList } from '../navigation/types';
import { useTheme } from '../hooks/useTheme';
import { useHabits } from '../hooks/useHabits';
import { useSubscription } from '../hooks/useSubscription';
import {
  sendMessage,
  getDailyCheckin,
  getWeeklySummary,
  getHabitRecommendations,
} from '../services/claude';
import {
  getConversations,
  saveConversation,
} from '../storage/conversations';
import { todayString } from '../utils/date';
import { ChatMessage } from '../types';
import PaywallModal from '../components/PaywallModal';

type Props = BottomTabScreenProps<MainTabsParamList, 'AIChat'>;

interface Bubble {
  id: string;
  role: 'user' | 'assistant';
  content: string;
}

const SUGGESTIONS = [
  'How am I doing?',
  'Suggest a new habit',
  'Weekly summary',
];

export default function AIChatScreen(_props: Props) {
  const theme = useTheme();
  const { habits, completions } = useHabits();
  const subscription = useSubscription();

  const [messages, setMessages] = useState<Bubble[]>([]);
  const [input, setInput] = useState('');
  const [sending, setSending] = useState(false);
  const [loading, setLoading] = useState(true);
  const [paywallVisible, setPaywallVisible] = useState(false);
  const [freeUsedThisSession, setFreeUsedThisSession] = useState(0);
  const [priorCount, setPriorCount] = useState(0);

  const listRef = useRef<FlatList<Bubble>>(null);

  useEffect(() => {
    (async () => {
      const convos = await getConversations();
      setPriorCount(convos.length);
      const flat: Bubble[] = [];
      convos.forEach((c) => {
        flat.push({ id: `${c.id}-u`, role: 'user', content: c.userMessage });
        flat.push({ id: `${c.id}-a`, role: 'assistant', content: c.aiResponse });
      });
      setMessages(flat);
      setLoading(false);
    })();
  }, []);

  // Total AI exchanges used (persisted prior + this session).
  const usedExchanges = priorCount + freeUsedThisSession;
  const gated = !subscription.isPremium && usedExchanges >= 1;

  const scrollToEnd = useCallback(() => {
    requestAnimationFrame(() => listRef.current?.scrollToEnd({ animated: true }));
  }, []);

  const runExchange = useCallback(
    async (userMessage: string) => {
      if (sending) return;
      if (gated) {
        setPaywallVisible(true);
        return;
      }

      const userBubble: Bubble = {
        id: `${Date.now()}-u`,
        role: 'user',
        content: userMessage,
      };
      // History from current messages BEFORE adding the new user message.
      const history: ChatMessage[] = messages.map((m) => ({
        role: m.role,
        content: m.content,
      }));

      setMessages((prev) => [...prev, userBubble]);
      setInput('');
      setSending(true);
      scrollToEnd();

      let aiResponse: string;
      try {
        if (userMessage === 'How am I doing?') {
          aiResponse = await getDailyCheckin(habits, completions);
        } else if (userMessage === 'Weekly summary') {
          aiResponse = await getWeeklySummary(habits, completions);
        } else if (userMessage === 'Suggest a new habit') {
          aiResponse = await getHabitRecommendations(habits);
        } else {
          aiResponse = await sendMessage(userMessage, history);
        }
      } catch (e) {
        aiResponse =
          "I'm having trouble reaching my brain right now 😅. Please check your connection or API key and try again.";
      }

      const aiBubble: Bubble = {
        id: `${Date.now()}-a`,
        role: 'assistant',
        content: aiResponse,
      };
      setMessages((prev) => [...prev, aiBubble]);
      setSending(false);
      setFreeUsedThisSession((n) => n + 1);
      scrollToEnd();

      try {
        await saveConversation({
          id: Date.now().toString(),
          date: todayString(),
          userMessage,
          aiResponse,
        });
      } catch {
        // non-fatal
      }
    },
    [sending, gated, messages, habits, completions, scrollToEnd]
  );

  const handleSend = useCallback(() => {
    const text = input.trim();
    if (!text) return;
    runExchange(text);
  }, [input, runExchange]);

  const renderItem = useCallback(
    ({ item }: { item: Bubble }) => {
      const isUser = item.role === 'user';
      return (
        <View
          style={[
            styles.bubbleRow,
            { justifyContent: isUser ? 'flex-end' : 'flex-start' },
          ]}
        >
          <View
            style={[
              styles.bubble,
              isUser
                ? { backgroundColor: theme.primary, borderBottomRightRadius: 4 }
                : {
                    backgroundColor: theme.card,
                    borderColor: theme.border,
                    borderWidth: StyleSheet.hairlineWidth,
                    borderBottomLeftRadius: 4,
                  },
            ]}
          >
            <Text
              style={[
                styles.bubbleText,
                { color: isUser ? theme.white : theme.text },
              ]}
            >
              {item.content}
            </Text>
          </View>
        </View>
      );
    },
    [theme]
  );

  if (loading) {
    return (
      <SafeAreaView
        style={[styles.center, { backgroundColor: theme.background }]}
      >
        <ActivityIndicator color={theme.primary} />
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={[styles.flex, { backgroundColor: theme.background }]} edges={['top']}>
      <KeyboardAvoidingView
        style={styles.flex}
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
      >
        <Text style={[styles.title, { color: theme.text }]}>AI Coach</Text>

        <FlatList
          ref={listRef}
          data={messages}
          keyExtractor={(m) => m.id}
          renderItem={renderItem}
          contentContainerStyle={styles.listContent}
          onContentSizeChange={scrollToEnd}
          ListEmptyComponent={
            <View style={styles.empty}>
              <Text style={styles.emptyEmoji}>💬</Text>
              <Text style={[styles.emptyTitle, { color: theme.text }]}>
                Your coach is ready
              </Text>
              <Text style={[styles.emptyText, { color: theme.textSecondary }]}>
                Ask anything about your habits, or tap a suggestion below.
              </Text>
            </View>
          }
          ListFooterComponent={
            sending ? (
              <View style={[styles.bubbleRow, { justifyContent: 'flex-start' }]}>
                <View
                  style={[
                    styles.bubble,
                    {
                      backgroundColor: theme.card,
                      borderColor: theme.border,
                      borderWidth: StyleSheet.hairlineWidth,
                    },
                  ]}
                >
                  <ActivityIndicator color={theme.textSecondary} size="small" />
                </View>
              </View>
            ) : null
          }
        />

        {gated && (
          <View
            style={[
              styles.gateCard,
              { backgroundColor: theme.primaryMuted, borderColor: theme.primary },
            ]}
          >
            <Text style={[styles.gateTitle, { color: theme.text }]}>
              Upgrade for unlimited AI coaching ✨
            </Text>
            <Text style={[styles.gateText, { color: theme.textSecondary }]}>
              You've used your free message. Go Premium for unlimited check-ins,
              summaries, and advice.
            </Text>
            <TouchableOpacity
              style={[styles.gateButton, { backgroundColor: theme.primary }]}
              onPress={() => setPaywallVisible(true)}
            >
              <Text style={[styles.gateButtonText, { color: theme.white }]}>
                Upgrade to Premium
              </Text>
            </TouchableOpacity>
          </View>
        )}

        {!gated && (
          <View style={styles.chipsRow}>
            {SUGGESTIONS.map((s) => (
              <TouchableOpacity
                key={s}
                style={[
                  styles.chip,
                  { backgroundColor: theme.cardElevated, borderColor: theme.border },
                ]}
                onPress={() => runExchange(s)}
                disabled={sending}
              >
                <Text style={[styles.chipText, { color: theme.text }]}>{s}</Text>
              </TouchableOpacity>
            ))}
          </View>
        )}

        <View style={[styles.inputRow, { borderTopColor: theme.border }]}>
          <TextInput
            style={[
              styles.input,
              {
                backgroundColor: theme.card,
                color: theme.text,
                borderColor: theme.border,
              },
            ]}
            placeholder={gated ? 'Upgrade to keep chatting…' : 'Message your coach…'}
            placeholderTextColor={theme.textSecondary}
            value={input}
            onChangeText={setInput}
            editable={!gated && !sending}
            multiline
          />
          <TouchableOpacity
            style={[
              styles.sendButton,
              {
                backgroundColor:
                  gated || sending || !input.trim()
                    ? theme.primaryMuted
                    : theme.primary,
              },
            ]}
            onPress={handleSend}
            disabled={gated || sending || !input.trim()}
          >
            <Text style={[styles.sendText, { color: theme.white }]}>➤</Text>
          </TouchableOpacity>
        </View>
      </KeyboardAvoidingView>

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

const styles = StyleSheet.create({
  flex: { flex: 1 },
  center: { flex: 1, alignItems: 'center', justifyContent: 'center' },
  title: {
    fontSize: 24,
    fontWeight: '800',
    paddingHorizontal: 20,
    paddingTop: 8,
    paddingBottom: 4,
  },
  listContent: { padding: 16, flexGrow: 1 },
  bubbleRow: { flexDirection: 'row', marginVertical: 4 },
  bubble: {
    maxWidth: '82%',
    borderRadius: 18,
    paddingHorizontal: 14,
    paddingVertical: 10,
  },
  bubbleText: { fontSize: 15, lineHeight: 21 },
  empty: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: 32 },
  emptyEmoji: { fontSize: 44, marginBottom: 12 },
  emptyTitle: { fontSize: 18, fontWeight: '700', marginBottom: 6 },
  emptyText: { fontSize: 14, textAlign: 'center', lineHeight: 20 },
  gateCard: {
    margin: 12,
    padding: 16,
    borderRadius: 16,
    borderWidth: 1,
  },
  gateTitle: { fontSize: 16, fontWeight: '700', marginBottom: 6 },
  gateText: { fontSize: 13, lineHeight: 19, marginBottom: 12 },
  gateButton: { borderRadius: 12, paddingVertical: 12, alignItems: 'center' },
  gateButtonText: { fontWeight: '700' },
  chipsRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
    paddingHorizontal: 12,
    paddingBottom: 8,
  },
  chip: {
    borderRadius: 18,
    borderWidth: 1,
    paddingHorizontal: 14,
    paddingVertical: 8,
  },
  chipText: { fontSize: 13, fontWeight: '600' },
  inputRow: {
    flexDirection: 'row',
    alignItems: 'flex-end',
    padding: 12,
    gap: 8,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  input: {
    flex: 1,
    borderRadius: 20,
    borderWidth: StyleSheet.hairlineWidth,
    paddingHorizontal: 16,
    paddingVertical: 10,
    fontSize: 15,
    maxHeight: 120,
  },
  sendButton: {
    width: 44,
    height: 44,
    borderRadius: 22,
    alignItems: 'center',
    justifyContent: 'center',
  },
  sendText: { fontSize: 18, fontWeight: '700' },
});
