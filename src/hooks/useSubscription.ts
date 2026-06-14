import { useCallback, useEffect, useState } from 'react';
import {
  isPremium as readPremium,
  purchasePremium as doPurchase,
  restorePurchases as doRestore,
  cancelPremium as doCancel,
  FREE_HABIT_LIMIT,
} from '../services/revenuecat';

export interface UseSubscription {
  isPremium: boolean;
  loading: boolean;
  freeHabitLimit: number;
  refresh: () => Promise<void>;
  purchase: () => Promise<boolean>;
  restore: () => Promise<boolean>;
  cancel: () => Promise<void>;
}

export function useSubscription(): UseSubscription {
  const [isPremium, setIsPremium] = useState(false);
  const [loading, setLoading] = useState(true);

  const refresh = useCallback(async () => {
    const premium = await readPremium();
    setIsPremium(premium);
    setLoading(false);
  }, []);

  useEffect(() => {
    refresh();
  }, [refresh]);

  const purchase = useCallback(async () => {
    const ok = await doPurchase();
    await refresh();
    return ok;
  }, [refresh]);

  const restore = useCallback(async () => {
    const ok = await doRestore();
    await refresh();
    return ok;
  }, [refresh]);

  const cancel = useCallback(async () => {
    await doCancel();
    await refresh();
  }, [refresh]);

  return {
    isPremium,
    loading,
    freeHabitLimit: FREE_HABIT_LIMIT,
    refresh,
    purchase,
    restore,
    cancel,
  };
}
