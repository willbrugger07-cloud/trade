using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Live "box break" streaming desk.
/// Player sells team spots to AI buyers upfront (instant revenue),
/// then opens the box live; each pulled card goes to whoever bought that team.
/// </summary>
public class BoxBreakDesk : MonoBehaviour, IInteractable
{
    [Header("Break Settings")]
    public float   spotPrice     = 50f;
    public int     totalSpots    = 10;
    public List<string> teamsList;      // e.g. "Lakers", "Cowboys", "Yankees"…

    [Header("Pack Source")]
    public CardPackSO packTemplate;
    public int        packsInBreak = 3;

    public bool IsBreakActive { get; private set; }

    private List<BreakSlot> _slots = new();

    [System.Serializable]
    public class BreakSlot
    {
        public string buyerName;
        public string assignedTeam;
    }

    // ----------------------------------------------------------------

    public void Interact(PlayerInteraction player)
    {
        if (IsBreakActive) { NotificationSystem.Show("Break already in progress!"); return; }
        BoxBreakUI.Instance?.Open(this);
    }

    // ----------------------------------------------------------------

    /// <summary>Sell all spots and collect revenue — call before opening.</summary>
    public void SellAllSpots()
    {
        if (IsBreakActive) return;
        _slots.Clear();

        int seats = Mathf.Min(totalSpots, teamsList.Count);
        for (int i = 0; i < seats; i++)
        {
            _slots.Add(new BreakSlot
            {
                buyerName    = $"User_{Random.Range(100, 999)}",
                assignedTeam = teamsList[i],
            });
            ShopManager.Instance.AddFunds(spotPrice);
        }

        IsBreakActive = true;
        NotificationSystem.Show($"🎰 All {seats} spots sold! ${spotPrice * seats:F0} collected. Open the box!");
        AudioManager.Play("break_sold_out");
    }

    /// <summary>Rip all packs and distribute cards to slot winners.</summary>
    public void ExecuteBreak()
    {
        if (!IsBreakActive) { NotificationSystem.Show("Sell spots first."); return; }

        var allPulled = new List<CardInstance>();
        for (int i = 0; i < packsInBreak; i++)
            allPulled.AddRange(packTemplate.OpenPack());

        foreach (var card in allPulled)
        {
            var winner = _slots[Random.Range(0, _slots.Count)];
            NotificationSystem.Show($"🎉 {card.GetDisplayName()} → {winner.buyerName} ({winner.assignedTeam})!");
            if (card.data.tier >= CardTier.Autograph)
                AudioManager.Play("big_pull");
        }

        IsBreakActive = false;
        _slots.Clear();
        BoxBreakUI.Instance?.Close();
    }
}
