using UnityEngine;

/// <summary>
/// Push-and-pull haggling for high-value showcase sales.
/// Triggered when a customer attempts to buy a PremiumDisplayCase card.
/// Player counter-offers via HagglingUI; AI adapts until patience runs out.
/// </summary>
public class HagglingSystem : MonoBehaviour
{
    public static HagglingSystem Instance { get; private set; }

    public CardInstance ActiveCard        { get; private set; }
    public float        StickerPrice      { get; private set; }
    public float        CustomerOffer     { get; private set; }
    public int          PatienceStrikes   { get; private set; }

    private float _minAcceptablePrice;
    private System.Action<float> _onSale;
    private System.Action        _onWalkout;

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ----------------------------------------------------------------

    public void Begin(CardInstance card, float stickerPrice,
                      System.Action<float> onSale, System.Action onWalkout)
    {
        ActiveCard    = card;
        StickerPrice  = stickerPrice;
        PatienceStrikes = 3;
        _onSale       = onSale;
        _onWalkout    = onWalkout;

        // Absolute floor: 90% of current market value
        _minAcceptablePrice = card.data.currentMarketValue * 0.90f;
        // Customer opens at 75% of sticker price
        CustomerOffer = Mathf.Round(stickerPrice * 0.75f * 100f) / 100f;

        HagglingUI.Instance?.Open(this);
        NotificationSystem.Show($"Customer offers ${CustomerOffer:F2} for {card.GetDisplayName()}");
    }

    // Player proposes a counter price
    public void CounterOffer(float proposed)
    {
        PatienceStrikes--;

        if (proposed <= _minAcceptablePrice)
        {
            Close(proposed);
            return;
        }

        if (PatienceStrikes <= 0)
        {
            NotificationSystem.Show("Customer got frustrated and left!");
            AudioManager.Play("customer_leave_angry");
            _onWalkout?.Invoke();
            HagglingUI.Instance?.Close();
            return;
        }

        float maxWilling = ActiveCard.data.currentMarketValue * 1.15f;
        if (proposed > maxWilling)
        {
            NotificationSystem.Show($"Too high — customer scoffs. {PatienceStrikes} chances left.");
            return;
        }

        // AI nudges upward
        CustomerOffer = Mathf.Round(Mathf.Lerp(CustomerOffer, proposed, 0.40f) * 100f) / 100f;
        HagglingUI.Instance?.Refresh(this);
        NotificationSystem.Show($"Customer nudges to ${CustomerOffer:F2}. {PatienceStrikes} patience left.");
    }

    public void AcceptCustomerOffer() => Close(CustomerOffer);

    private void Close(float finalPrice)
    {
        ShopManager.Instance.AddFunds(finalPrice);
        NotificationSystem.Show($"✅ Sold {ActiveCard.GetDisplayName()} for ${finalPrice:F2}!");
        AudioManager.Play("register_cha_ching");
        _onSale?.Invoke(finalPrice);
        HagglingUI.Instance?.Close();
        ActiveCard = null;
    }
}
