using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks per-customer loyalty points and issues tier rewards.
/// Customers identify via a loyalty card number; points accumulate across visits.
/// </summary>
public class LoyaltyRewardApp : MonoBehaviour
{
    public static LoyaltyRewardApp Instance { get; private set; }

    public enum LoyaltyTier { Bronze, Silver, Gold, Platinum }

    [Serializable]
    public class LoyaltyAccount
    {
        public string customerId;
        public string displayName;
        public int    points;
        public LoyaltyTier tier;
        public int    totalVisits;
    }

    [Header("Tier Thresholds")]
    public int silverThreshold   = 200;
    public int goldThreshold     = 600;
    public int platinumThreshold = 1500;

    [Header("Rewards")]
    public float bronzeDiscountPct   = 0.00f;
    public float silverDiscountPct   = 0.03f;
    public float goldDiscountPct     = 0.06f;
    public float platinumDiscountPct = 0.10f;

    private readonly Dictionary<string, LoyaltyAccount> _accounts = new();

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ----------------------------------------------------------------

    public LoyaltyAccount GetOrCreate(string customerId, string displayName)
    {
        if (!_accounts.TryGetValue(customerId, out var acct))
        {
            acct = new LoyaltyAccount { customerId = customerId, displayName = displayName };
            _accounts[customerId] = acct;
        }
        return acct;
    }

    public void RecordPurchase(string customerId, float saleTotal)
    {
        if (!_accounts.TryGetValue(customerId, out var acct)) return;

        int earned = Mathf.FloorToInt(saleTotal);
        acct.points      += earned;
        acct.totalVisits += 1;
        UpdateTier(acct);
        NotificationSystem.Show($"⭐ {acct.displayName} earned {earned} pts ({acct.points} total, {acct.tier})");
    }

    public float GetDiscountMultiplier(string customerId)
    {
        if (!_accounts.TryGetValue(customerId, out var acct)) return 1f;
        return acct.tier switch
        {
            LoyaltyTier.Silver   => 1f - silverDiscountPct,
            LoyaltyTier.Gold     => 1f - goldDiscountPct,
            LoyaltyTier.Platinum => 1f - platinumDiscountPct,
            _                    => 1f,
        };
    }

    public bool RedeemPoints(string customerId, int cost, out float cashValue)
    {
        cashValue = 0f;
        if (!_accounts.TryGetValue(customerId, out var acct) || acct.points < cost) return false;
        acct.points -= cost;
        cashValue    = cost * 0.01f;  // 100 pts = $1
        UpdateTier(acct);
        return true;
    }

    private void UpdateTier(LoyaltyAccount acct)
    {
        var prev = acct.tier;
        acct.tier = acct.points >= platinumThreshold ? LoyaltyTier.Platinum
                  : acct.points >= goldThreshold     ? LoyaltyTier.Gold
                  : acct.points >= silverThreshold   ? LoyaltyTier.Silver
                                                     : LoyaltyTier.Bronze;
        if (acct.tier != prev)
            NotificationSystem.Show($"🎖 {acct.displayName} upgraded to {acct.tier}!");
    }
}
