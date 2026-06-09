using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Bulk auto-pricing machine. Player dumps cards into the hopper;
/// machine scans each one, applies market price minus a configurable
/// margin, and routes them to back-room inventory with price tags set.
/// </summary>
public class AutoSorterMachine : MonoBehaviour, IInteractable
{
    public static AutoSorterMachine Instance { get; private set; }

    [Header("Settings")]
    [Range(0f, 0.5f)] public float marginDiscount = 0.10f;   // sell 10% below market
    public float secondsPerCard = 0.4f;

    [Header("State")]
    public List<CardInstance> Hopper { get; private set; } = new();
    public bool IsBusy { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ----------------------------------------------------------------

    public void Interact(PlayerInteraction player) =>
        AutoSorterUI.Instance?.Open(this);

    public void LoadCard(CardInstance card)
    {
        Hopper.Add(card);
        InventoryManager.Instance.RemoveFromBackRoom(card);
        NotificationSystem.Show($"Added {card.data.cardName} to hopper ({Hopper.Count} queued).");
    }

    public void StartSorting()
    {
        if (IsBusy || Hopper.Count == 0) return;
        StartCoroutine(SortRoutine());
    }

    private IEnumerator SortRoutine()
    {
        IsBusy = true;
        AudioManager.Play("machine_hum");
        int sorted = 0;

        while (Hopper.Count > 0)
        {
            var card = Hopper[0];
            Hopper.RemoveAt(0);

            float price = card.GetSalePrice() * (1f - marginDiscount);
            card.overridePrice = price;
            InventoryManager.Instance.AddToBackRoom(card);
            sorted++;

            AutoSorterUI.Instance?.RefreshHopper(Hopper);
            yield return new WaitForSeconds(secondsPerCard);
        }

        IsBusy = false;
        AudioManager.Play("machine_done");
        NotificationSystem.Show($"✅ Auto-sorted {sorted} cards. Prices set at -{marginDiscount*100:F0}% market.");
    }
}
