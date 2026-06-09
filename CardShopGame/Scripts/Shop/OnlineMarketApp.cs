using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// In-game e-commerce platform (tablet app).
/// Player lists cards; simulated internet bidders compete; player packs and ships.
/// 10% platform fee on all sales.
/// </summary>
public class OnlineMarketApp : MonoBehaviour
{
    public static OnlineMarketApp Instance { get; private set; }

    [Header("Shipping")]
    public GameObject shippingBoxPrefab;
    public Transform  shippingOutboxPoint;

    [Range(0f, 0.5f)] public float platformFeeRate = 0.10f;

    public IReadOnlyList<OnlineListing> Listings => _listings;
    private readonly List<OnlineListing> _listings = new();

    [Serializable]
    public class OnlineListing
    {
        public CardInstance card;
        public float        currentBid;
        public int          hoursRemaining;
    }

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ----------------------------------------------------------------

    public void ListCard(CardInstance card, float startPrice, int durationDays = 3)
    {
        if (!InventoryManager.Instance.RemoveFromBackRoom(card))
        {
            NotificationSystem.Show("Card not in back room.");
            return;
        }

        float fee = Mathf.Round(startPrice * platformFeeRate * 100f) / 100f;
        ShopManager.Instance.SpendFunds(fee);

        var listing = new OnlineListing
        {
            card           = card,
            currentBid     = startPrice,
            hoursRemaining = durationDays * 24,
        };
        _listings.Add(listing);
        StartCoroutine(SimulateBidding(listing));
        NotificationSystem.Show($"📡 Listed {card.GetDisplayName()} online at ${startPrice:F2} (fee ${fee:F2})");
    }

    private IEnumerator SimulateBidding(OnlineListing listing)
    {
        while (listing.hoursRemaining > 0)
        {
            yield return new WaitForSeconds(Random.Range(5f, 20f));
            listing.hoursRemaining -= 4;

            float market = listing.card.GetSalePrice();
            if (listing.currentBid < market * 1.1f)
            {
                listing.currentBid += Mathf.Round(Random.Range(5f, 25f) * 100f) / 100f;
            }
        }

        CloseListing(listing);
    }

    private void CloseListing(OnlineListing listing)
    {
        _listings.Remove(listing);

        float payout = Mathf.Round(listing.currentBid * (1f - platformFeeRate) * 100f) / 100f;
        ShopManager.Instance.AddFunds(payout);

        // Spawn a shipping box the player must fill and drop at the outbox
        if (shippingBoxPrefab != null)
        {
            var box  = Instantiate(shippingBoxPrefab, shippingOutboxPoint.position, Quaternion.identity);
            var comp = box.GetComponent<ShippingBox>();
            comp?.Setup(listing.card);
        }

        NotificationSystem.Show($"📦 Online sale closed: ${listing.currentBid:F2} → net ${payout:F2}. Pack and ship!");
        AudioManager.Play("online_sale");
    }
}
