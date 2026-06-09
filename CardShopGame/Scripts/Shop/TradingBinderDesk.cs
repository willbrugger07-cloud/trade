using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Interactive card-for-card trade desk.
/// AI customers walk up carrying a binder of their own cards.
/// Player scrolls through, picks what they want, offers store stock in return.
/// AI accepts if the trade value is within ~5% of fair.
/// </summary>
public class TradingBinderDesk : MonoBehaviour, IInteractable
{
    public List<CardInstance> CustomerBinder    { get; private set; } = new();
    public List<CardInstance> PlayerOfferBasket { get; private set; } = new();
    public List<CardInstance> PlayerWantBasket  { get; private set; } = new();

    public bool SessionActive => CustomerBinder.Count > 0;

    private System.Action _onSessionEnd;

    // ----------------------------------------------------------------

    public void Interact(PlayerInteraction player) =>
        TradeBinderUI.Instance?.Open(this);

    // ----------------------------------------------------------------

    /// <summary>Called by a trade-in AI customer when they walk up.</summary>
    public void OpenSession(List<CardInstance> customerCards, System.Action onEnd)
    {
        CustomerBinder.Clear();
        CustomerBinder.AddRange(customerCards);
        PlayerOfferBasket.Clear();
        PlayerWantBasket.Clear();
        _onSessionEnd = onEnd;

        TradeBinderUI.Instance?.Open(this);
        NotificationSystem.Show("📖 Customer trade binder opened. Find the gems!");
        AudioManager.Play("card_place_on_mat");
    }

    // UI calls these:
    public void WantCardFromBinder(CardInstance card)
    {
        if (CustomerBinder.Contains(card) && !PlayerWantBasket.Contains(card))
            PlayerWantBasket.Add(card);
    }

    public void OfferFromStock(CardInstance card)
    {
        if (!PlayerOfferBasket.Contains(card))
            PlayerOfferBasket.Add(card);
    }

    public void RemoveFromOffer(CardInstance card) => PlayerOfferBasket.Remove(card);
    public void RemoveFromWant(CardInstance card)  => PlayerWantBasket.Remove(card);

    // ----------------------------------------------------------------

    public void SubmitTrade()
    {
        float giving  = PlayerOfferBasket.Sum(c => c.GetSalePrice());
        float taking  = PlayerWantBasket.Sum(c => c.GetSalePrice());

        // Customer accepts if they're getting fair value (within 5% of equal)
        if (giving >= taking * 0.95f)
        {
            foreach (var card in PlayerWantBasket)
            {
                CustomerBinder.Remove(card);
                InventoryManager.Instance.AddToBackRoom(card);
            }
            foreach (var card in PlayerOfferBasket)
                InventoryManager.Instance.RemoveFromBackRoom(card);

            NotificationSystem.Show($"🤝 Trade accepted! Net value gain: ${(taking - giving):F2}");
            AudioManager.Play("register_cha_ching");
            ReputationManager.Instance?.RecordPositiveEvent(2f);
            CloseSession();
        }
        else
        {
            NotificationSystem.Show("❌ Collector rejected the trade — they're losing value.");
            AudioManager.Play("customer_leave_angry");
        }
    }

    public void CloseSession()
    {
        CustomerBinder.Clear();
        PlayerOfferBasket.Clear();
        PlayerWantBasket.Clear();
        _onSessionEnd?.Invoke();
        _onSessionEnd = null;
        TradeBinderUI.Instance?.Close();
    }
}
