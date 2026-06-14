import React, { useEffect, useRef } from 'react';
import { Animated, StyleSheet, Text, View } from 'react-native';
import { useTheme } from '../hooks/useTheme';

interface CompletionAnimationProps {
  visible: boolean;
  onDone?: () => void;
}

export default function CompletionAnimation({
  visible,
  onDone,
}: CompletionAnimationProps) {
  const theme = useTheme();
  const scale = useRef(new Animated.Value(0)).current;
  const opacity = useRef(new Animated.Value(0)).current;
  const [animating, setAnimating] = React.useState(false);

  useEffect(() => {
    if (!visible) return;

    setAnimating(true);
    scale.setValue(0);
    opacity.setValue(0);

    const animation = Animated.sequence([
      Animated.parallel([
        Animated.spring(scale, {
          toValue: 1,
          friction: 5,
          tension: 80,
          useNativeDriver: true,
        }),
        Animated.timing(opacity, {
          toValue: 1,
          duration: 180,
          useNativeDriver: true,
        }),
      ]),
      Animated.delay(550),
      Animated.parallel([
        Animated.timing(opacity, {
          toValue: 0,
          duration: 250,
          useNativeDriver: true,
        }),
        Animated.timing(scale, {
          toValue: 1.2,
          duration: 250,
          useNativeDriver: true,
        }),
      ]),
    ]);

    animation.start(({ finished }) => {
      if (finished) {
        setAnimating(false);
        onDone?.();
      }
    });

    return () => {
      animation.stop();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [visible]);

  if (!visible && !animating) return null;

  return (
    <View style={styles.overlay} pointerEvents="none">
      <Animated.View
        style={[
          styles.circle,
          { backgroundColor: theme.success, opacity, transform: [{ scale }] },
        ]}
      >
        <Text style={styles.check}>✓</Text>
      </Animated.View>
    </View>
  );
}

const styles = StyleSheet.create({
  overlay: {
    ...StyleSheet.absoluteFillObject,
    alignItems: 'center',
    justifyContent: 'center',
    zIndex: 1000,
  },
  circle: {
    width: 120,
    height: 120,
    borderRadius: 60,
    alignItems: 'center',
    justifyContent: 'center',
  },
  check: {
    color: '#FFFFFF',
    fontSize: 64,
    fontWeight: '900',
  },
});
