using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// In-store wholesale ordering computer.
/// Player interacts with it to order booster boxes or single packs.
/// The physical delivery box spawns at the loading dock after a short delay.
/// </summary>
public class OrderTerminal : MonoBehaviour, IInteractable
{
    [Serializable]
    public struct CatalogItem
    {
        public string      itemName;
        public CardPackSO  packData;          // which pack template to open
        public int         packsPerBox;       // how many packs in the box
        public float       wholesaleCost;
        public GameObject  deliveryBoxPrefab; // physical prop that spawns outside
    }

    [Header("Catalog")]
    public List<CatalogItem> catalog;

    [Header("Delivery")]
    public Transform deliveryDropPoint;
    public float     deliveryDelaySeconds = 5f;   // simulate shipping time

    // ----------------------------------------------------------------

    public void Interact(PlayerInteraction player)
    {
        // In a real build this opens an in-game UI canvas.
        // For prototyping, we call PurchaseItem directly.
        OrderTerminalUI.Instance?.Open(this);
    }

    public void PurchaseItem(int catalogIndex)
    {
        if (catalogIndex < 0 || catalogIndex >= catalog.Count) return;
        var item = catalog[catalogIndex];

        if (!ShopManager.Instance.SpendFunds(item.wholesaleCost, "wholesale"))
            return;

        StartCoroutine(DeliverBox(item));
        NotificationSystem.Show($"Ordered: {item.itemName} — arrives shortly.");
        AudioManager.Play("order_confirm");
    }

    private IEnumerator DeliverBox(CatalogItem item)
    {
        yield return new WaitForSeconds(deliveryDelaySeconds);

        if (item.deliveryBoxPrefab != null)
            Instantiate(item.deliveryBoxPrefab, deliveryDropPoint.position, Quaternion.identity);

        // Pre-populate a DeliveryBox component so the player can unbox it
        // (DeliveryBox opens the packs when the player picks it up)
        AudioManager.Play("delivery_arrive");
        NotificationSystem.Show($"📦 {item.itemName} delivered to loading dock!");
    }
}
