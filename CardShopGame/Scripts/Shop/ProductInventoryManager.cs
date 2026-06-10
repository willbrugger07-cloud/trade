using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages the store's product catalog across all box tiers.
/// Handles ordering, shelf placement routing, and the Breaker's Delight
/// live-stream trigger flow.
/// </summary>
public class ProductInventoryManager : MonoBehaviour
{
    public static ProductInventoryManager Instance { get; private set; }

    [Header("Catalog")]
    public List<ProductConfigData> availableConfigs;

    private readonly Dictionary<ProductConfigData, int> _stock = new();

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ----------------------------------------------------------------

    /// <summary>
    /// Wholesale cost spikes during peak season (demand >= 1.4×) — buying
    /// off-season and holding is the intended strategy.
    /// Fire sale: end-of-season (day 14 of 15 cycle) cuts cost by 25%.
    /// </summary>
    public float GetAdjustedWholesaleCost(ProductConfigData config)
    {
        float base_ = config.wholesaleCost;
        if (SportsSeasonManager.Instance == null) return base_;

        var sport = CardSport.Basketball;
        foreach (CardSport s in System.Enum.GetValues(typeof(CardSport)))
            if (config.parentSeriesName.Contains(s.ToString())) { sport = s; break; }

        float demand = SportsSeasonManager.Instance.GetDemandMultiplier(sport);

        // Peak season: wholesale is expensive
        if (demand >= 1.40f) base_ *= 1.30f;

        // Fire sale: last day of season, clear out at 25% off
        int daysLeft = 15 - (DayCycleManager.Instance.CurrentDay % 15);
        if (daysLeft == 1 && demand >= 1.40f) base_ *= 0.75f;

        return Mathf.Round(base_ * 100f) / 100f;
    }

    public bool OrderStock(ProductConfigData config, int qty)
    {
        if (config == null || qty <= 0) return false;

        float unitCost = GetAdjustedWholesaleCost(config);
        float total    = Mathf.Round(unitCost * qty * 100f) / 100f;
        if (!ShopManager.Instance.SpendFunds(total, $"Order {qty}x {config.parentSeriesName} {config.boxFormat}"))
            return false;

        if (!_stock.ContainsKey(config)) _stock[config] = 0;
        _stock[config] += qty;

        NotificationSystem.Show($"📦 Ordered {qty}x {config.boxFormat} ({config.parentSeriesName}). Total: ${total:F2}");
        return true;
    }

    public int GetStock(ProductConfigData config) =>
        _stock.TryGetValue(config, out int n) ? n : 0;

    // ----------------------------------------------------------------

    public Transform GetShelfTargetForConfig(ProductConfigData config)
    {
        return config.boxFormat switch
        {
            ProductConfigData.PackFormat.HangerBox =>
                FindObjectsOfType<CardShelf>()
                    .FirstOrDefault(s => s.shelfType == CardShelf.ShelfType.PegRack && s.NeedsRestock)
                    ?.transform,

            ProductConfigData.PackFormat.HobbyBox =>
                FindObjectsOfType<PremiumDisplayCase>()
                    .FirstOrDefault(d => d.FeaturedCard == null)
                    ?.transform,

            ProductConfigData.PackFormat.BreakersDelight =>
                FindObjectOfType<BoxBreakDesk>()?.transform,

            _ =>
                FindObjectsOfType<CardShelf>()
                    .FirstOrDefault(s => s.shelfType == CardShelf.ShelfType.Standard && s.NeedsRestock)
                    ?.transform,
        };
    }

    // ----------------------------------------------------------------

    public List<CardInstance> OpenSinglePack(ProductConfigData config)
    {
        if (GetStock(config) <= 0)
        {
            NotificationSystem.Show("No stock of that product.");
            return new List<CardInstance>();
        }

        _stock[config]--;
        var cards = GeneratePack(config);

        if (config.boxFormat == ProductConfigData.PackFormat.BreakersDelight)
        {
            StreamingRigManager.Instance?.StartBroadcast();
            StreamingRigManager.Instance?.SpikeChatOnHit(50);
        }

        PackOpeningJuiceDirector.Instance?.BeginRevealSequence(cards);
        return cards;
    }

    // ----------------------------------------------------------------

    private List<CardInstance> GeneratePack(ProductConfigData config)
    {
        var results = new List<CardInstance>();

        if (config.boxFormat == ProductConfigData.PackFormat.BreakersDelight)
        {
            for (int i = 0; i < config.cardsPerPack; i++)
                results.Add(RollPremiumHit(forceOnCard: true));
            return results;
        }

        int hitSlots = config.guaranteedAutographs + config.guaranteedMemorabiliaPatches;
        for (int i = 0; i < hitSlots && i < config.cardsPerPack; i++)
            results.Add(RollPremiumHit(config.boxFormat == ProductConfigData.PackFormat.HobbyBox));

        for (int i = results.Count; i < config.cardsPerPack; i++)
        {
            float roll = Random.value;

            if (roll < config.exclusiveParallelChance && !string.IsNullOrEmpty(config.exclusiveParallelName))
                results.Add(RollExclusiveParallel(config.exclusiveParallelName));
            else if (roll < 0.02f)
                results.Add(RollPremiumHit(false));
            else
                results.Add(RollBaseCard());
        }

        return results;
    }

    // ----------------------------------------------------------------

    private List<SportsCard> _cardPool;

    private SportsCard RandomFromPool()
    {
        if (_cardPool == null || _cardPool.Count == 0)
            _cardPool = new List<SportsCard>(Resources.FindObjectsOfTypeAll<SportsCard>());
        return _cardPool.Count > 0 ? _cardPool[Random.Range(0, _cardPool.Count)] : null;
    }

    private CardInstance RollBaseCard()
    {
        var data = RandomFromPool();
        return new CardInstance
        {
            data         = data,
            serialNumber = "0",
            layout       = new CardVisualLayout { setType = CardVisualLayout.SetStyle.PaperBase },
        };
    }

    private CardInstance RollExclusiveParallel(string parallelName)
    {
        var c = RollBaseCard();
        c.layout.setType        = CardVisualLayout.SetStyle.ChromeFoil;
        c.layout.cardBorderGlow = new Color(1f, 0.5f, 0f);
        c.serialNumber          = Random.Range(1, 100).ToString();
        return c;
    }

    private CardInstance RollPremiumHit(bool forceOnCard)
    {
        var c = RollBaseCard();
        c.isAutographed   = true;
        c.layout.inkStyle = forceOnCard
            ? CardVisualLayout.AutoType.OnCardInk
            : CardVisualLayout.AutoType.StickerAuto;

        bool oneOfOne  = Random.value > 0.97f;
        c.serialNumber = oneOfOne ? "1" : Random.Range(1, 100).ToString();

        if (oneOfOne)
        {
            c.layout.memorabiliaStyle = CardVisualLayout.PatchType.LogomanTag;
            c.layout.setType          = CardVisualLayout.SetStyle.AcetateClear;
            c.layout.cardBorderGlow   = Color.yellow;
            if (c.data) c.data.tier   = CardTier.Immortal;
        }
        else
        {
            c.layout.setType = CardVisualLayout.SetStyle.ChromeFoil;
        }

        return c;
    }

    // ----------------------------------------------------------------

    public List<ProductAdvisory> GetBuyingAdvisory()
    {
        var list = new List<ProductAdvisory>();
        foreach (var config in availableConfigs)
        {
            float demand = 1f;
            if (SportsSeasonManager.Instance != null)
            {
                var sport = CardSport.Basketball;
                foreach (CardSport s in System.Enum.GetValues(typeof(CardSport)))
                    if (config.parentSeriesName.Contains(s.ToString())) { sport = s; break; }
                demand = SportsSeasonManager.Instance.GetDemandMultiplier(sport);
            }

            float roi = demand * config.GetCollectorAppealMultiplier();
            list.Add(new ProductAdvisory
            {
                config  = config,
                demand  = demand,
                roi     = Mathf.Round(roi * 100f) / 100f,
                inStock = GetStock(config),
            });
        }
        list.Sort((a, b) => b.roi.CompareTo(a.roi));
        return list;
    }

    [System.Serializable]
    public class ProductAdvisory
    {
        public ProductConfigData config;
        public float             demand;
        public float             roi;
        public int               inStock;
    }
}
