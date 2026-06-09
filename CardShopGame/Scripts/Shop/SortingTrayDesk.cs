using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Physical sorting tray. Player drags loose cards into category slots;
/// PackageSlot bundles them for shelf or back-room routing.
/// </summary>
public class SortingTrayDesk : MonoBehaviour, IInteractable
{
    public List<CardInstance> rookieSlot    = new();
    public List<CardInstance> autographSlot = new();
    public List<CardInstance> insertSlot    = new();
    public List<CardInstance> baseSlot      = new();

    // ----------------------------------------------------------------

    public void Interact(PlayerInteraction player) =>
        SortingTrayUI.Instance?.Open(this);

    public void DropCard(CardInstance card, string slot)
    {
        GetSlot(slot).Add(card);
        AudioManager.Play("card_place_on_mat");
        NotificationSystem.Show($"Sorted {card.data.cardName} → {slot}");
    }

    public List<CardInstance> PackageSlot(string slot)
    {
        var target = GetSlot(slot);
        var bundle = new List<CardInstance>(target);
        target.Clear();
        return bundle;
    }

    private List<CardInstance> GetSlot(string slot) =>
        slot.ToLower() switch
        {
            "rookie"    => rookieSlot,
            "auto"      => autographSlot,
            "insert"    => insertSlot,
            _           => baseSlot,
        };
}
