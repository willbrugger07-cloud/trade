using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tablet/terminal app for wholesale liquidation auctions.
/// Generates daily lots procedurally; player bids or clicks Buy-It-Now.
/// Won lots spawn as a cargo pallet at the delivery dock.
/// </summary>
public class CardBrokerApp : MonoBehaviour
{
    public static CardBrokerApp Instance { get; private set; }

    [Header("Delivery")]
    public GameObject palletPrefab;
    public Transform  deliveryDock;

    public IReadOnlyList<AuctionLot> Listings => _listings;
    private readonly List<AuctionLot> _listings = new();

    [Serializable]
    public class AuctionLot
    {
        public string title;
        public int    cardCount;
        public float  currentBid;
        public float  buyItNowPrice;
        public int    hoursRemaining;
        public CardPackSO packTemplate;   // used to generate cards when won
    }

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()  => DayCycleManager.OnNewDay += PopulateDailyLots;
    private void OnDisable() => DayCycleManager.OnNewDay -= PopulateDailyLots;

    // ----------------------------------------------------------------

    private void PopulateDailyLots()
    {
        _listings.Clear();
        _listings.Add(new AuctionLot
        {
            title         = "Storage Locker: 90s Basketball Bulk",
            cardCount     = 500,
            currentBid    = 120f,
            buyItNowPrice = 250f,
            hoursRemaining = 8,
        });
        _listings.Add(new AuctionLot
        {
            title         = "Estate Sale: High-End Football Inserts",
            cardCount     = 25,
            currentBid    = 450f,
            buyItNowPrice = 700f,
            hoursRemaining = 4,
        });
        NotificationSystem.Show("📋 New wholesale lots available in the Broker app.");
    }

    public void PlaceBid(int index, float amount)
    {
        if (index < 0 || index >= _listings.Count) return;
        var lot = _listings[index];
        if (amount <= lot.currentBid) { NotificationSystem.Show("Bid must beat current price."); return; }
        if (!ShopManager.Instance.SpendFunds(amount - lot.currentBid)) return;
        lot.currentBid = amount;
        NotificationSystem.Show($"Bid placed: ${amount:F2} on "{lot.title}"");
    }

    public void BuyItNow(int index)
    {
        if (index < 0 || index >= _listings.Count) return;
        var lot = _listings[index];
        if (!ShopManager.Instance.SpendFunds(lot.buyItNowPrice)) return;

        DeliverLot(lot);
        _listings.RemoveAt(index);
    }

    private void DeliverLot(AuctionLot lot)
    {
        if (palletPrefab && deliveryDock)
            Instantiate(palletPrefab, deliveryDock.position, Quaternion.identity);

        // Pre-generate cards if pack template exists
        if (lot.packTemplate != null)
        {
            int packs = Mathf.CeilToInt(lot.cardCount / 5f);
            var all   = new List<CardInstance>();
            for (int i = 0; i < packs; i++) all.AddRange(lot.packTemplate.OpenPack());
            InventoryManager.Instance.AddRangeToBackRoom(all);
        }

        NotificationSystem.Show($"📦 "{lot.title}" delivered to loading dock!");
        AudioManager.Play("delivery_arrive");
    }
}
