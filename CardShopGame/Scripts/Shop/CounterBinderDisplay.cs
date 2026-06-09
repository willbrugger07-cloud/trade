using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Open binder on the checkout counter. Customers have a chance to grab
/// a card as an impulse buy while waiting in line.
/// </summary>
public class CounterBinderDisplay : MonoBehaviour, IInteractable
{
    public List<CardInstance> pages     = new();
    public int                maxSlots  = 18;   // two 9-pocket sheets
    [Range(0f, 1f)] public float impulseChance = 0.25f;

    // ----------------------------------------------------------------

    public void Interact(PlayerInteraction player) =>
        CounterBinderUI.Instance?.Open(this);

    public bool SlotCard(CardInstance card)
    {
        if (pages.Count >= maxSlots) return false;
        pages.Add(card);
        return true;
    }

    /// <summary>Called by CheckoutRegister for each customer transaction.</summary>
    public CardInstance RollImpulsePurchase()
    {
        if (pages.Count == 0 || Random.value > impulseChance) return null;
        int idx  = Random.Range(0, pages.Count);
        var card = pages[idx];
        pages.RemoveAt(idx);
        NotificationSystem.Show($"💳 Impulse buy: {card.data.cardName} (${card.GetSalePrice():F2})");
        return card;
    }
}
