import 'react-native-gesture-handler';
import 'react-native-get-random-values';
import React, { useEffect, useMemo, useState, useCallback } from 'react';
import { ActivityIndicator, View, Text, useColorScheme } from 'react-native';
import { StatusBar } from 'expo-status-bar';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import {
  NavigationContainer,
  DefaultTheme,
  DarkTheme,
} from '@react-navigation/native';
import { createStackNavigator } from '@react-navigation/stack';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';

import { RootStackParamList, MainTabsParamList } from './src/navigation/types';
import {
  ThemeContext,
  DarkModeOverride,
} from './src/hooks/useTheme';
import { lightTheme, darkTheme } from './src/constants/colors';
import { getSettings } from './src/storage/settings';

import OnboardingScreen from './src/screens/OnboardingScreen';
import DashboardScreen from './src/screens/DashboardScreen';
import AddHabitScreen from './src/screens/AddHabitScreen';
import HabitDetailScreen from './src/screens/HabitDetailScreen';
import AIChatScreen from './src/screens/AIChatScreen';
import SettingsScreen from './src/screens/SettingsScreen';
import PaywallScreen from './src/screens/PaywallScreen';

const RootStack = createStackNavigator<RootStackParamList>();
const Tabs = createBottomTabNavigator<MainTabsParamList>();

function TabIcon({ label, focused }: { label: string; focused: boolean }) {
  return (
    <Text style={{ fontSize: 20, opacity: focused ? 1 : 0.5 }}>{label}</Text>
  );
}

function MainTabs() {
  return (
    <ThemeConsumerTabs />
  );
}

function ThemeConsumerTabs() {
  return (
    <ThemeContext.Consumer>
      {({ theme }) => (
        <Tabs.Navigator
          screenOptions={{
            headerStyle: { backgroundColor: theme.card },
            headerTitleStyle: { color: theme.text },
            headerShadowVisible: false,
            tabBarStyle: {
              backgroundColor: theme.card,
              borderTopColor: theme.border,
            },
            tabBarActiveTintColor: theme.primary,
            tabBarInactiveTintColor: theme.textSecondary,
          }}
        >
          <Tabs.Screen
            name="Dashboard"
            component={DashboardScreen}
            options={{
              headerShown: false,
              tabBarIcon: ({ focused }) => (
                <TabIcon label="🏠" focused={focused} />
              ),
            }}
          />
          <Tabs.Screen
            name="AIChat"
            component={AIChatScreen}
            options={{
              title: 'AI Coach',
              tabBarLabel: 'Coach',
              tabBarIcon: ({ focused }) => (
                <TabIcon label="💬" focused={focused} />
              ),
            }}
          />
          <Tabs.Screen
            name="Settings"
            component={SettingsScreen}
            options={{
              tabBarIcon: ({ focused }) => (
                <TabIcon label="⚙️" focused={focused} />
              ),
            }}
          />
        </Tabs.Navigator>
      )}
    </ThemeContext.Consumer>
  );
}

export default function App() {
  const systemScheme = useColorScheme();
  const [override, setOverride] = useState<DarkModeOverride>(null);
  const [booting, setBooting] = useState(true);
  const [onboarded, setOnboarded] = useState(false);

  useEffect(() => {
    (async () => {
      const settings = await getSettings();
      setOnboarded(settings.onboardingComplete);
      setOverride(settings.darkModeOverride ?? null);
      setBooting(false);
    })();
  }, []);

  const isDark =
    override === 'dark' || (override === null && systemScheme === 'dark');
  const theme = isDark ? darkTheme : lightTheme;

  const themeValue = useMemo(
    () => ({ theme, override, setOverride }),
    [theme, override]
  );

  const navTheme = useMemo(() => {
    const base = isDark ? DarkTheme : DefaultTheme;
    return {
      ...base,
      colors: {
        ...base.colors,
        primary: theme.primary,
        background: theme.background,
        card: theme.card,
        text: theme.text,
        border: theme.border,
      },
    };
  }, [isDark, theme]);

  const handleOnboardingComplete = useCallback(() => setOnboarded(true), []);

  if (booting) {
    return (
      <View
        style={{
          flex: 1,
          alignItems: 'center',
          justifyContent: 'center',
          backgroundColor: theme.background,
        }}
      >
        <ActivityIndicator size="large" color={theme.primary} />
      </View>
    );
  }

  return (
    <SafeAreaProvider>
      <ThemeContext.Provider value={themeValue}>
        <StatusBar style={isDark ? 'light' : 'dark'} />
        <NavigationContainer theme={navTheme}>
          <RootStack.Navigator
            screenOptions={{
              headerStyle: { backgroundColor: theme.card },
              headerTitleStyle: { color: theme.text },
              headerTintColor: theme.primary,
              headerShadowVisible: false,
            }}
          >
            {!onboarded ? (
              <RootStack.Screen name="Onboarding" options={{ headerShown: false }}>
                {() => (
                  <OnboardingScreen onComplete={handleOnboardingComplete} />
                )}
              </RootStack.Screen>
            ) : (
              <>
                <RootStack.Screen
                  name="MainTabs"
                  component={MainTabs}
                  options={{ headerShown: false }}
                />
                <RootStack.Screen
                  name="AddHabit"
                  component={AddHabitScreen}
                  options={{ title: 'New Habit', presentation: 'modal' }}
                />
                <RootStack.Screen
                  name="HabitDetail"
                  component={HabitDetailScreen}
                  options={{ title: 'Habit' }}
                />
                <RootStack.Screen
                  name="Paywall"
                  component={PaywallScreen}
                  options={{ headerShown: false, presentation: 'modal' }}
                />
              </>
            )}
          </RootStack.Navigator>
        </NavigationContainer>
      </ThemeContext.Provider>
    </SafeAreaProvider>
  );
}
