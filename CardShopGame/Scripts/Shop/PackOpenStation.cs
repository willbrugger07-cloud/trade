using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Counter-top pack-ripping station. Player opens packs here;
/// Ripper customer archetype also uses this to buy and rip on-site.
/// </summary>
public class PackOpenStation : MonoBehaviour, IInteractable
{
    [Header("Available Packs")]
    public List<CardPackSO> packsForSale;

    [Header("Pack Price Markup")]
    [Range(1f, 3f)] public float retailMarkup = 1.5f;

    // ----------------------------------------------------------------

    public void Interact(PlayerInteraction player) =>
        PackStationUI.Instance?.Open(this);

    // ----------------------------------------------------------------

    /// <summary>Player opens a pack from their own inventory.</summary>
    public List<CardInstance> PlayerOpenPack(CardPackSO pack)
    {
        var cards = pack.OpenPack();
        foreach (var c in cards)
        {
            if (c.data.tier >= CardTier.Autograph)
            {
                NotificationSystem.Show($"🔥 BIG HIT: {c.GetDisplayName()}!");
                AudioManager.Play("big_pull");
            }
        }
        InventoryManager.Instance.AddRangeToBackRoom(cards);
        return cards;
    }

    /// <summary>
    /// Ripper customer buys and rips a random available pack.
    /// Revenue goes to the shop. Cards belong to the customer (they leave with them).
    /// </summary>
    public List<CardInstance> CustomerRipPack(float budget)
    {
        var affordable = packsForSale.FindAll(
            p => p.wholesaleCost * retailMarkup <= budget);

        if (affordable.Count == 0) return new List<CardInstance>();

        var chosen = affordable[Random.Range(0, affordable.Count)];
        float price = chosen.wholesaleCost * retailMarkup;
        ShopManager.Instance.AddFunds(price);

        return chosen.OpenPack();
    }
}
