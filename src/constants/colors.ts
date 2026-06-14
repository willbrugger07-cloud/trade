import { Category } from '../types';

export interface Theme {
  primary: string;
  primaryMuted: string;
  background: string;
  card: string;
  cardElevated: string;
  text: string;
  textSecondary: string;
  border: string;
  success: string;
  streak: string;
  danger: string;
  white: string;
  shadow: string;
  isDark: boolean;
}

export const PALETTE = {
  primary: '#6C63FF',
  success: '#4CAF50',
  streak: '#FF9F43',
  danger: '#FF5252',
  white: '#FFFFFF',
};

export const lightTheme: Theme = {
  primary: '#6C63FF',
  primaryMuted: '#EAE8FF',
  background: '#F8F9FA',
  card: '#FFFFFF',
  cardElevated: '#FFFFFF',
  text: '#1A1A2E',
  textSecondary: '#6B7280',
  border: '#E5E7EB',
  success: '#4CAF50',
  streak: '#FF9F43',
  danger: '#FF5252',
  white: '#FFFFFF',
  shadow: '#000000',
  isDark: false,
};

export const darkTheme: Theme = {
  primary: '#8B82FF',
  primaryMuted: '#2A2740',
  background: '#0D1117',
  card: '#161B22',
  cardElevated: '#1F2630',
  text: '#E6EDF3',
  textSecondary: '#8B949E',
  border: '#30363D',
  success: '#4CAF50',
  streak: '#FF9F43',
  danger: '#FF6B6B',
  white: '#FFFFFF',
  shadow: '#000000',
  isDark: true,
};

export const CATEGORY_COLORS: Record<Category, string> = {
  health: '#FF6B6B',
  fitness: '#4ECDC4',
  mindfulness: '#A8E6CF',
  productivity: '#FFD93D',
  finance: '#6BCB77',
};
