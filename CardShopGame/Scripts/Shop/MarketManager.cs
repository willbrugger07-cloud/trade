using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simulates a live card market. Every in-game day, each card's value
/// fluctuates based on a "player performance" random roll.
/// Big positive swings represent a breakout game; negative swings a slump.
/// </summary>
public class MarketManager : MonoBehaviour
{
    public static MarketManager Instance { get; private set; }

    [Header("All Cards In The Economy")]
    public List<SportsCard> activeCards;

    [Header("Daily Swing Limits")]
    [Range(0f, 0.5f)] public float maxDownswing = 0.10f;  // -10% floor
    [Range(0f, 1f)]   public float maxUpswing   = 0.25f;  // +25% ceiling
    [Range(0f, 1f)]   public float hotStreak    = 0.05f;  // chance of 2× upswing

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()  => DayCycleManager.OnNewDay += FluctuateMarket;
    private void OnDisable() => DayCycleManager.OnNewDay -= FluctuateMarket;

    private void FluctuateMarket()
    {
        foreach (var card in activeCards)
        {
            float swing = Random.Range(-maxDownswing, maxUpswing);

            // Occasional hot-streak doubles the upswing (simulates big game)
            if (swing > 0 && Random.value < hotStreak) swing *= 2f;

            card.currentMarketValue *= (1f + swing);
            card.currentMarketValue  = Mathf.Max(0.01f,
                Mathf.Round(card.currentMarketValue * 100f) / 100f);
        }

        Debug.Log($"[Market] Day {DayCycleManager.Instance.CurrentDay} prices updated.");
    }

    /// <summary>Force a spike on a specific card (e.g., trade-in influx).</summary>
    public void ApplySpike(SportsCard card, float multiplier)
    {
        card.currentMarketValue = Mathf.Round(card.currentMarketValue * multiplier * 100f) / 100f;
        NotificationSystem.Show($"📈 {card.playerName}'s card value spiked!");
    }
}
