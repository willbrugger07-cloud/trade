using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Standard retail shelf — holds multiple copies of a single card type.
/// Player stocks it from back-room inventory; customers pick cards from it.
/// </summary>
public class CardShelf : MonoBehaviour, IInteractable
{
    public enum ShelfType { Standard, PegRack, GlassCase }

    [Header("Configuration")]
    public ShelfType shelfType     = ShelfType.Standard;
    public int       maxCapacity   = 10;
    public float     retailPrice;              // price tag on this shelf slot
    public Transform customerInteractionPoint;

    // Raised when stock level changes — ShelfLabelSlot subscribes
    public event Action OnShelfStockChanged;

    // The card type assigned to this shelf (can be null for unassigned slots)
    public SportsCard stockedCard { get; private set; }

    private readonly List<CardInstance> _stock = new();
    public IReadOnlyList<CardInstance> Stock   => _stock;

    public bool HasStock     => _stock.Count > 0;
    public bool NeedsRestock => _stock.Count < maxCapacity / 2;
    public int  StockCount   => _stock.Count;
    public int  currentStock => _stock.Count;

    [Header("Visuals")]
    public List<GameObject> stackVisuals;

    // ----------------------------------------------------------------

    public bool StockCard(CardInstance card)
    {
        if (_stock.Count >= maxCapacity)
        {
            NotificationSystem.Show("Shelf is full!");
            return false;
        }

        if (stockedCard == null) stockedCard = card.data;
        if (retailPrice  <= 0f)  retailPrice  = card.GetSalePrice();

        _stock.Add(card);
        InventoryManager.Instance.RemoveFromBackRoom(card);
        Refresh();
        return true;
    }

    /// <summary>Called by RestockerAI when it carries a PhysicalBox to this shelf.</summary>
    public void RestockFromBox(PhysicalBox box)
    {
        if (box == null) return;
        while (!box.IsEmpty && _stock.Count < maxCapacity)
        {
            var card = box.ExtractOne();
            if (card == null) break;
            _stock.Add(card);
        }
        Refresh();
    }

    public CardInstance TakeCard()
    {
        if (!HasStock) return null;
        var card = _stock[_stock.Count - 1];
        _stock.RemoveAt(_stock.Count - 1);
        Refresh();
        return card;
    }

    public void Interact(PlayerInteraction player)
    {
        InventoryUI.Instance?.OpenForShelf(this);
    }

    private void Refresh()
    {
        for (int i = 0; i < stackVisuals.Count; i++)
            stackVisuals[i].SetActive(i < _stock.Count);
        OnShelfStockChanged?.Invoke();
    }
}
