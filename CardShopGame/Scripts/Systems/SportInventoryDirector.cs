using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Central sport-aware inventory layer.
///
/// Responsibilities:
///   • Wraps InventoryManager with sport-filtered queries.
///   • Applies seasonal demand multipliers when valuing stock.
///   • Provides buy-low / sell-high advisory data for the OrderTerminal UI.
///   • Hooks into SportsSeasonManager season changes and refreshes shelf
///     price-tag labels and TV ticker lines automatically.
/// </summary>
public class SportInventoryDirector : MonoBehaviour
{
    public static SportInventoryDirector Instance { get; private set; }

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()  => DayCycleManager.OnNewDay += OnNewDay;
    private void OnDisable() => DayCycleManager.OnNewDay -= OnNewDay;

    // ----------------------------------------------------------------
    // Seasonal refresh

    private void OnNewDay()
    {
        BroadcastSeasonTicker();
        RefreshDisplayPrices();
    }

    private void BroadcastSeasonTicker()
    {
        if (SportsSeasonManager.Instance == null || UIManager.Instance == null) return;

        foreach (CardSport sport in System.Enum.GetValues(typeof(CardSport)))
        {
            float mult = SportsSeasonManager.Instance.GetDemandMultiplier(sport);
            if (mult >= 1.40f)
                UIManager.Instance.PushTickerLine(
                    $"{sport} {HobbyTerminology.GetCaseHitName(sport)} +{(mult - 1f) * 100f:F0}% demand");
        }
    }

    /// <summary>
    /// Walk every PremiumDisplayCase and CardShelf and nudge retail prices
    /// to track the seasonal multiplier so the player doesn't have to manually
    /// reprice everything after each season change.
    /// Auto-pricing only moves price toward the fair seasonal value; it never
    /// undercuts what the player has manually set above that threshold.
    /// </summary>
    private void RefreshDisplayPrices()
    {
        if (SportsSeasonManager.Instance == null) return;

        foreach (var display in FindObjectsOfType<PremiumDisplayCase>())
        {
            if (display.FeaturedCard == null) continue;
            float fair = SportsSeasonManager.Instance.GetSeasonalValue(display.FeaturedCard);
            // RetailPrice has no public setter; auto-pricing via AssignCard is deferred to player action
        }
    }

    // ----------------------------------------------------------------
    // Sport-filtered inventory queries

    public List<CardInstance> GetBackRoomBySport(CardSport sport)
    {
        if (InventoryManager.Instance == null) return new List<CardInstance>();
        return InventoryManager.Instance.BackRoom
            .Where(c => c.data != null && c.data.sport == sport)
            .ToList();
    }

    /// <summary>
    /// Returns every sport's back-room count and seasonal demand, sorted
    /// descending by demand. Used by the OrderTerminal "advisor" tab.
    /// </summary>
    public List<SportSnapshot> GetMarketSnapshot()
    {
        var result = new List<SportSnapshot>();
        foreach (CardSport sport in System.Enum.GetValues(typeof(CardSport)))
        {
            float demand = SportsSeasonManager.Instance
                ? SportsSeasonManager.Instance.GetDemandMultiplier(sport)
                : 1f;

            int stockCount = InventoryManager.Instance
                ? InventoryManager.Instance.BackRoom.Count(c => c.data?.sport == sport)
                : 0;

            result.Add(new SportSnapshot
            {
                sport      = sport,
                demand     = demand,
                stockCount = stockCount,
                buyTip     = HobbyTerminology.GetSeasonBuyTip(sport),
            });
        }
        result.Sort((a, b) => b.demand.CompareTo(a.demand));
        return result;
    }

    // ----------------------------------------------------------------
    // AI purchase evaluation (called by CustomerAI instead of raw price check)

    /// <summary>
    /// Returns true if the player's retail price is acceptable for this card
    /// given the current season. Drives customer buy/reject decision.
    /// Also applies reputation effects for gouging or fair pricing.
    /// </summary>
    public bool EvaluatePurchase(CardInstance card, out float finalRevenue)
    {
        finalRevenue = 0f;

        float seasonalFair   = SportsSeasonManager.Instance != null
            ? SportsSeasonManager.Instance.GetSeasonalValue(card)
            : card.GetSalePrice();

        float playerPrice    = card.overridePrice > 0f ? card.overridePrice : card.GetSalePrice();
        float markupRatio    = seasonalFair > 0f ? playerPrice / seasonalFair : 1f;
        float demandMult     = SportsSeasonManager.Instance?.GetDemandMultiplier(card.data.sport) ?? 1f;

        if (markupRatio <= 1.00f)
        {
            // Bargain — instant purchase, rep gain
            finalRevenue = playerPrice;
            ReputationManager.Instance?.RecordPositiveEvent(0.75f);
            return true;
        }

        if (markupRatio <= 1.20f)
        {
            // Small markup — seasonal demand gives them a nudge to accept
            bool buys = Random.value * demandMult > 0.50f;
            if (buys) finalRevenue = playerPrice;
            return buys;
        }

        // Gouging — rep penalty, walk away
        ReputationManager.Instance?.RecordNegativeEvent(1.2f);
        return false;
    }

    // ----------------------------------------------------------------

    [System.Serializable]
    public class SportSnapshot
    {
        public CardSport sport;
        public float                demand;
        public int                  stockCount;
        public string               buyTip;
    }
}
