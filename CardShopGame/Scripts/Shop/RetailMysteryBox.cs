using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A sealed mystery box prop. Customers purchase it from a shelf;
/// a Ripper customer may open it in-store.
/// </summary>
public class RetailMysteryBox : MonoBehaviour, IInteractable
{
    public string            BoxName     { get; private set; }
    public float             RetailPrice { get; private set; }
    private List<CardInstance> _contents;

    public void Setup(string name, float price,
                      List<CardInstance> bulk, CardInstance hit)
    {
        BoxName     = name;
        RetailPrice = price;
        _contents   = new List<CardInstance>(bulk) { hit };
    }

    // Customer / player opens the box
    public void Interact(PlayerInteraction player)
    {
        NotificationSystem.Show($"Opening {BoxName}…");
        foreach (var c in _contents)
            NotificationSystem.Show($"  Pulled: {c.GetDisplayName()} (${c.GetSalePrice():F2})");
        AudioManager.Play("box_open");
        Destroy(gameObject, 0.3f);
    }

    // Called by CustomerAI (Ripper) — no PlayerInteraction reference
    public List<CardInstance> OpenAsCustomer()
    {
        var result = new List<CardInstance>(_contents);
        Destroy(gameObject, 0.3f);
        return result;
    }
}
