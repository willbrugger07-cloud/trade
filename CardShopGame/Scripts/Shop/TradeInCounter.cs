using UnityEngine;

/// <summary>
/// Walk-up buying counter. AI customers lay down a card and ask a price.
/// Player can Accept, Haggle, or Decline via the UI.
/// </summary>
public class TradeInCounter : MonoBehaviour, IInteractable
{
    public CardInstance OfferedCard     { get; private set; }
    public float        AskingPrice     { get; private set; }
    public bool         IsOccupied      => OfferedCard != null;

    private System.Action _onComplete;

    // ----------------------------------------------------------------

    /// <summary>Called by a TradeIn customer AI when they walk up.</summary>
    public void PresentCard(CardInstance card, System.Action onComplete)
    {
        if (IsOccupied) return;

        OfferedCard  = card;
        _onComplete  = onComplete;

        float market = card.GetSalePrice();
        AskingPrice  = Mathf.Round(market * Random.Range(0.60f, 0.80f) * 100f) / 100f;

        TradeInUI.Instance?.Show(this);
        AudioManager.Play("tradein_walk_up");
    }

    // IInteractable — player walks up if UI didn't auto-open
    public void Interact(PlayerInteraction player) =>
        TradeInUI.Instance?.Show(this);

    // ----------------------------------------------------------------

    public void Accept()
    {
        if (!IsOccupied) return;
        if (!ShopManager.Instance.SpendFunds(AskingPrice)) return;

        InventoryManager.Instance.AddToBackRoom(OfferedCard);
        NotificationSystem.Show($"Bought {OfferedCard.GetDisplayName()} for ${AskingPrice:F2}");
        AudioManager.Play("register_cha_ching");
        Conclude();
    }

    /// <param name="offer">Counter-offer from the player.</param>
    public void Haggle(float offer)
    {
        if (!IsOccupied) return;
        float market = OfferedCard.GetSalePrice();

        if (offer >= market * 0.50f)
        {
            ShopManager.Instance.SpendFunds(offer);
            InventoryManager.Instance.AddToBackRoom(OfferedCard);
            NotificationSystem.Show($"Counter-offer accepted! Bought for ${offer:F2}");
            AudioManager.Play("register_cha_ching");
        }
        else
        {
            NotificationSystem.Show("Customer was offended — they walked out.");
            AudioManager.Play("customer_leave_angry");
        }
        Conclude();
    }

    public void Decline()
    {
        NotificationSystem.Show("Offer declined.");
        AudioManager.Play("trade_in_decline");
        Conclude();
    }

    private void Conclude()
    {
        TradeInUI.Instance?.Hide();
        _onComplete?.Invoke();
        OfferedCard = null;
        AskingPrice = 0f;
    }
}
