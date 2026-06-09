using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PriceHistoryTracker
{
    public CardInstance trackedCard;
    public List<float>  history = new();  // rolling 7-day closes

    public void RecordClose()
    {
        if (trackedCard == null) return;
        history.Add(trackedCard.GetSalePrice());
        if (history.Count > 7) history.RemoveAt(0);
    }

    public float TrendPercent()
    {
        if (history.Count < 2) return 0f;
        float first = history[0];
        float last  = history[^1];
        return first == 0f ? 0f : (last - first) / first * 100f;
    }
}

/// <summary>
/// Wall display that spotlights up to 5 cards and shows a 7-day price trend.
/// Renders trend data via SpeculationWallUI each day.
/// </summary>
public class SpeculationWall : MonoBehaviour, IInteractable
{
    public int maxSlots = 5;
    public List<PriceHistoryTracker> slots = new();

    // ----------------------------------------------------------------

    private void OnEnable()  => DayCycleManager.OnShopClose += RecordClosingPrices;
    private void OnDisable() => DayCycleManager.OnShopClose -= RecordClosingPrices;

    public void Interact(PlayerInteraction player) =>
        SpeculationWallUI.Instance?.Open(this);

    // ----------------------------------------------------------------

    public bool MountCard(CardInstance card, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSlots) return false;

        while (slots.Count <= slotIndex) slots.Add(new PriceHistoryTracker());

        slots[slotIndex] = new PriceHistoryTracker { trackedCard = card };
        slots[slotIndex].RecordClose();

        NotificationSystem.Show($"📈 {card.data.cardName} added to Speculation Wall (slot {slotIndex + 1}).");
        return true;
    }

    public void UnmountCard(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < slots.Count)
            slots[slotIndex] = new PriceHistoryTracker();
    }

    private void RecordClosingPrices()
    {
        foreach (var tracker in slots) tracker.RecordClose();
        SpeculationWallUI.Instance?.Refresh(slots);
    }
}
