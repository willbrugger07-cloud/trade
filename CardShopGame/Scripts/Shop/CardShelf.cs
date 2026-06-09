using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Standard retail shelf — holds multiple copies of a single card type.
/// Player stocks it from back-room inventory; customers pick cards from it.
/// </summary>
public class CardShelf : MonoBehaviour, IInteractable
{
    [Header("Configuration")]
    public int maxCapacity = 10;
    public Transform customerInteractionPoint;

    // Runtime state
    private readonly List<CardInstance> _stock = new();
    public IReadOnlyList<CardInstance> Stock => _stock;

    public bool HasStock    => _stock.Count > 0;
    public bool NeedsRestock => _stock.Count < maxCapacity / 2;
    public int  StockCount  => _stock.Count;

    [Header("Visuals")]
    [Tooltip("Child objects that represent a stacked card on the shelf")]
    public List<GameObject> stackVisuals;

    // ----------------------------------------------------------------

    /// <summary>Called by player to move a card from back room to shelf.</summary>
    public bool StockCard(CardInstance card)
    {
        if (_stock.Count >= maxCapacity)
        {
            NotificationSystem.Show("Shelf is full!");
            return false;
        }
        _stock.Add(card);
        InventoryManager.Instance.RemoveFromBackRoom(card);
        RefreshVisuals();
        return true;
    }

    /// <summary>Called by CustomerAI to take one card.</summary>
    public CardInstance TakeCard()
    {
        if (!HasStock) return null;
        var card = _stock[_stock.Count - 1];
        _stock.RemoveAt(_stock.Count - 1);
        RefreshVisuals();
        return card;
    }

    // IInteractable — player presses E near shelf
    public void Interact(PlayerInteraction player)
    {
        // Open the restocking UI for this shelf
        InventoryUI.Instance?.OpenForShelf(this);
    }

    private void RefreshVisuals()
    {
        for (int i = 0; i < stackVisuals.Count; i++)
            stackVisuals[i].SetActive(i < _stock.Count);
    }
}
