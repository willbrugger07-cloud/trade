using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages the back-room stock of card instances.
/// Separate from what is displayed on shelves / display cases.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    private readonly List<CardInstance> _backRoom = new();

    public IReadOnlyList<CardInstance> BackRoom => _backRoom;

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ----------------------------------------------------------------

    public void AddToBackRoom(CardInstance card)
    {
        _backRoom.Add(card);
        NotificationSystem.Show($"{card.GetDisplayName()} added to back room.");
    }

    public void AddRangeToBackRoom(IEnumerable<CardInstance> cards)
    {
        foreach (var c in cards) _backRoom.Add(c);
    }

    public bool RemoveFromBackRoom(CardInstance card)
    {
        return _backRoom.Remove(card);
    }

    /// <summary>
    /// Convenience used by CustomerAI Ripper archetype —
    /// returns a random sealed pack from inventory.
    /// </summary>
    public CardInstance GetRandomSealedPack()
    {
        // Packs would be tagged or stored separately; simplified here
        return _backRoom.Count > 0 ? _backRoom[Random.Range(0, _backRoom.Count)] : null;
    }

    public int TotalCards => _backRoom.Count;

    public float EstimatedInventoryValue =>
        _backRoom.Sum(c => c.GetSalePrice());
}
