using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Physical delivery box prop. Player picks it up (E key) to unbox it.
/// Generates CardInstance objects and sends them to InventoryManager.
/// Can hold multiple packs from the same CardPackSO.
/// </summary>
public class DeliveryBox : MonoBehaviour, IInteractable
{
    public CardPackSO packTemplate;
    public int        packsInBox = 12;
    public bool       isOpened   = false;

    public void Interact(PlayerInteraction player)
    {
        if (isOpened)
        {
            NotificationSystem.Show("Box is already empty.");
            return;
        }

        // Offer player choice: open all packs now (back room), or sell sealed
        UnboxAll();
    }

    private void UnboxAll()
    {
        isOpened = true;
        var allCards = new List<CardInstance>();

        for (int i = 0; i < packsInBox; i++)
            allCards.AddRange(packTemplate.OpenPack());

        InventoryManager.Instance.AddRangeToBackRoom(allCards);
        AudioManager.Play("box_open");
        NotificationSystem.Show($"Unboxed {allCards.Count} cards into back room!");

        Destroy(gameObject, 0.5f);
    }
}
