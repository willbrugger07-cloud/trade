using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bulk-card mystery box crafter.
/// Player assembles low-value singles + one guaranteed hit into a sealed
/// retail mystery box sold at a flat markup price.
/// </summary>
public class MysteryBoxStation : MonoBehaviour, IInteractable
{
    [Header("Minimum Requirements")]
    public int minBulkCards = 5;

    [Header("Prefab")]
    public GameObject sealedBoxPrefab;
    public Transform  boxingPoint;

    // In-progress box state
    private string            _boxName;
    private float             _retailPrice;
    private List<CardInstance> _bulk      = new();
    private CardInstance       _hitCard;

    public bool HasHitCard   => _hitCard != null;
    public int  BulkCount    => _bulk.Count;

    // ----------------------------------------------------------------

    public void Interact(PlayerInteraction player) =>
        MysteryBoxUI.Instance?.Open(this);

    // ----------------------------------------------------------------

    public void StartBox(string name, float price)
    {
        _boxName     = name;
        _retailPrice = price;
        _bulk.Clear();
        _hitCard = null;
        NotificationSystem.Show($"Building mystery box: "{name}" @ ${price:F2}");
    }

    public void AddCard(CardInstance card)
    {
        if (_boxName == null) { NotificationSystem.Show("Start a box first."); return; }

        InventoryManager.Instance.RemoveFromBackRoom(card);

        // Cards worth $50+ become the chase hit
        if (card.GetSalePrice() >= 50f && _hitCard == null)
        {
            _hitCard = card;
            NotificationSystem.Show($"⭐ Chase card: {card.GetDisplayName()}");
        }
        else
        {
            _bulk.Add(card);
            NotificationSystem.Show($"Added bulk card ({_bulk.Count}/{minBulkCards} min)");
        }
    }

    public void SealBox()
    {
        if (_bulk.Count < minBulkCards)
        {
            NotificationSystem.Show($"Need at least {minBulkCards} bulk cards (have {_bulk.Count}).");
            return;
        }
        if (_hitCard == null)
        {
            NotificationSystem.Show("Mystery boxes require at least one hit card ($50+).");
            return;
        }

        var go   = Object.Instantiate(sealedBoxPrefab, boxingPoint.position, Quaternion.identity);
        var box  = go.GetComponent<RetailMysteryBox>();
        box?.Setup(_boxName, _retailPrice, new List<CardInstance>(_bulk), _hitCard);

        _bulk.Clear();
        _hitCard = null;
        _boxName = null;

        AudioManager.Play("box_seal");
        NotificationSystem.Show("📦 Mystery box sealed! Move it to a shelf.");
    }
}
