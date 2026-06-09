using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Open bulk bin table. Customers sift through and buy loose base cards
/// at a flat per-card price. Pile mesh scales with card count.
/// </summary>
public class BulkCardBin : MonoBehaviour, IInteractable
{
    public List<CardInstance> cards       = new();
    public float              pricePerCard = 0.50f;
    public int                maxCapacity  = 200;

    [Header("Visual")]
    public Transform customerSiftPoint;
    public List<GameObject> pileMeshLODs;   // LOD0=full, LOD1=half, LOD2=empty

    // ----------------------------------------------------------------

    public void Interact(PlayerInteraction player) =>
        BulkBinUI.Instance?.Open(this);

    public bool LoadCards(List<CardInstance> incoming)
    {
        if (cards.Count + incoming.Count > maxCapacity)
        {
            NotificationSystem.Show($"Bin full! ({maxCapacity} max)");
            return false;
        }
        cards.AddRange(incoming);
        UpdateVisual();
        return true;
    }

    /// <summary>Called by CustomerAI when browsing the bin.</summary>
    public int SimulateSift(int patience)
    {
        if (cards.Count == 0) return 0;
        int take = Random.Range(1, Mathf.Min(8, cards.Count + 1));
        float revenue = take * pricePerCard;
        cards.RemoveRange(cards.Count - take, take);
        ShopManager.Instance.AddFunds(revenue);
        UpdateVisual();
        NotificationSystem.Show($"Customer bought {take} bulk cards (${revenue:F2}).");
        return take;
    }

    private void UpdateVisual()
    {
        if (pileMeshLODs == null || pileMeshLODs.Count == 0) return;
        float fill = (float)cards.Count / maxCapacity;
        for (int i = 0; i < pileMeshLODs.Count; i++)
            if (pileMeshLODs[i]) pileMeshLODs[i].SetActive(fill > (float)i / pileMeshLODs.Count);
    }

    private float Sum(List<float> list) { float s = 0; foreach (var v in list) s += v; return s; }
}
