using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TradeBounty
{
    public string          id;
    public string          customerName;
    public SportsCard      requestedCard;
    public float           reward;
    public int             daysLeft;
}

/// <summary>
/// Corkboard community wishlist. AI customers post bounties;
/// player fulfills them for a premium payout and rep boost.
/// </summary>
public class CommunityTradingBoard : MonoBehaviour, IInteractable
{
    public static CommunityTradingBoard Instance { get; private set; }

    public List<TradeBounty> bounties   = new();
    public int               maxBounties = 6;

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()  => DayCycleManager.OnNewDay += TickAndGenerateBounties;
    private void OnDisable() => DayCycleManager.OnNewDay -= TickAndGenerateBounties;

    public void Interact(PlayerInteraction player) =>
        TradingBoardUI.Instance?.Open(this);

    // ----------------------------------------------------------------

    private void TickAndGenerateBounties()
    {
        for (int i = bounties.Count - 1; i >= 0; i--)
        {
            bounties[i].daysLeft--;
            if (bounties[i].daysLeft <= 0) bounties.RemoveAt(i);
        }

        // Chance to post a new bounty each morning
        if (bounties.Count < maxBounties && Random.value < 0.5f)
            PostRandomBounty();
    }

    private void PostRandomBounty()
    {
        var pool = Resources.FindObjectsOfTypeAll<SportsCard>();
        if (pool.Length == 0) return;

        var card   = pool[Random.Range(0, pool.Length)];
        float reward = Mathf.Round(card.currentMarketValue * 1.25f * 100f) / 100f;

        bounties.Add(new TradeBounty
        {
            id           = Guid.NewGuid().ToString(),
            customerName = $"Collector_{Random.Range(100, 999)}",
            requestedCard = card,
            reward       = reward,
            daysLeft     = Random.Range(2, 6),
        });

        NotificationSystem.Show($"📌 New bounty: {card.cardName} — ${reward:F2}");
    }

    public bool FulfillBounty(string id, CardInstance submitted)
    {
        var bounty = bounties.Find(b => b.id == id);
        if (bounty == null || submitted.data != bounty.requestedCard) return false;

        InventoryManager.Instance.RemoveFromBackRoom(submitted);
        ShopManager.Instance.AddFunds(bounty.reward);
        ReputationManager.Instance?.RecordPositiveEvent(2f);
        bounties.Remove(bounty);

        NotificationSystem.Show($"🏅 Bounty fulfilled! +${bounty.reward:F2}");
        AudioManager.Play("register_cha_ching");
        return true;
    }
}
