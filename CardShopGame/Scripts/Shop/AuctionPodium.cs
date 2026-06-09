using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// In-store live auction. Host a timed event where AI customers bid against
/// each other. Whale archetypes push the price past market value.
/// Player can also place manual bids via AuctionUI.
/// </summary>
public class AuctionPodium : MonoBehaviour, IInteractable
{
    [Header("Timing")]
    public float auctionDurationSeconds = 30f;
    public float bidIntervalMin = 2f;
    public float bidIntervalMax = 5f;

    [Header("AI Aggression")]
    [Range(1f, 1.5f)]
    public float whaleMaxMultiplier = 1.25f;  // whales bid up to 125% of market value

    public CardInstance AuctionCard      { get; private set; }
    public float        CurrentBid       { get; private set; }
    public float        TimeRemaining    { get; private set; }
    public bool         IsRunning        { get; private set; }

    private readonly List<Transform> _crowdPoints = new();

    // ----------------------------------------------------------------

    public void Interact(PlayerInteraction player) =>
        AuctionUI.Instance?.Open(this);

    // ----------------------------------------------------------------

    /// <summary>Start the auction. Call from AuctionUI after player confirms.</summary>
    public bool StartAuction(CardInstance card, float reservePrice)
    {
        if (IsRunning) { NotificationSystem.Show("Auction already running!"); return false; }
        if (card == null) return false;

        AuctionCard  = card;
        CurrentBid   = reservePrice;
        InventoryManager.Instance.RemoveFromBackRoom(card);

        IsRunning = true;
        StartCoroutine(AuctionRoutine());
        AudioManager.Play("auction_gavel_start");
        NotificationSystem.Show($"🔨 Auction started for {card.GetDisplayName()} — reserve ${reservePrice:F2}!");
        return true;
    }

    // Player raises the bid manually via AuctionUI
    public void PlacePlayerBid(float amount)
    {
        if (!IsRunning || amount <= CurrentBid) return;
        CurrentBid = amount;
        AuctionUI.Instance?.UpdateBid(CurrentBid);
        AudioManager.Play("auction_bid");
    }

    // ----------------------------------------------------------------

    private IEnumerator AuctionRoutine()
    {
        TimeRemaining = auctionDurationSeconds;

        while (TimeRemaining > 0)
        {
            float wait = Random.Range(bidIntervalMin, bidIntervalMax);
            yield return new WaitForSeconds(wait);
            TimeRemaining -= wait;
            AuctionUI.Instance?.UpdateTimer(TimeRemaining);

            TryAIBid();
        }

        EndAuction();
    }

    private void TryAIBid()
    {
        float market = AuctionCard.GetSalePrice();
        if (CurrentBid >= market * whaleMaxMultiplier) return;

        float raise = Mathf.Round(Random.Range(10f, 55f));
        CurrentBid += raise;
        AuctionUI.Instance?.UpdateBid(CurrentBid);
        AudioManager.Play("auction_bid");
        NotificationSystem.Show($"🙋 New bid: ${CurrentBid:F2}");
    }

    private void EndAuction()
    {
        IsRunning = false;
        ShopManager.Instance.AddFunds(CurrentBid);
        AudioManager.Play("auction_gavel_end");
        NotificationSystem.Show($"🔨 SOLD for ${CurrentBid:F2}!");
        AuctionUI.Instance?.Close();
        AuctionCard = null;
    }
}
