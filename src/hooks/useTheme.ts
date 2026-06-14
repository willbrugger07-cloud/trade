import { createContext, useContext } from 'react';
import { Theme, lightTheme } from '../constants/colors';

export type DarkModeOverride = 'light' | 'dark' | null;

export interface ThemeContextValue {
  theme: Theme;
  override: DarkModeOverride;
  setOverride: (value: DarkModeOverride) => void;
}

export const ThemeContext = createContext<ThemeContextValue>({
  theme: lightTheme,
  override: null,
  setOverride: () => {},
});

export function useTheme(): Theme {
  return useContext(ThemeContext).theme;
}

export function useThemeControls(): ThemeContextValue {
  return useContext(ThemeContext);
}
