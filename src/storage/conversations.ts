import AsyncStorage from '@react-native-async-storage/async-storage';
import { AIConversation } from '../types';
import { STORAGE_KEYS } from '../constants/config';

export async function getConversations(): Promise<AIConversation[]> {
  try {
    const raw = await AsyncStorage.getItem(STORAGE_KEYS.conversations);
    if (!raw) return [];
    const parsed = JSON.parse(raw);
    return Array.isArray(parsed) ? (parsed as AIConversation[]) : [];
  } catch (e) {
    console.warn('getConversations failed', e);
    return [];
  }
}

export async function saveConversation(
  conversation: AIConversation
): Promise<void> {
  const conversations = await getConversations();
  conversations.push(conversation);
  await AsyncStorage.setItem(
    STORAGE_KEYS.conversations,
    JSON.stringify(conversations)
  );
}

export async function clearConversations(): Promise<void> {
  await AsyncStorage.removeItem(STORAGE_KEYS.conversations);
}
