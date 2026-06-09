using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simulates a weekly sports headline feed.
/// Events spike or crash specific player card values across the economy.
/// Headlines scroll on the in-world TV display and the tablet ticker.
/// </summary>
public class SportsTickerManager : MonoBehaviour
{
    public static SportsTickerManager Instance { get; private set; }

    [Header("Headline Pool")]
    public List<SportHeadline> headlines;

    [Header("Cards In Economy")]
    public List<SportsCard> allCards;

    public event Action<SportHeadline> OnHeadlineBroadcast;

    [Serializable]
    public class SportHeadline
    {
        public string headline;
        public string targetPlayerName;
        [Range(0.3f, 2.5f)] public float priceMultiplier;
    }

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()  => DayCycleManager.OnNewDay += TryFireWeeklyEvent;
    private void OnDisable() => DayCycleManager.OnNewDay -= TryFireWeeklyEvent;

    private void TryFireWeeklyEvent()
    {
        // Fire a headline every ~3 days on average
        if (Random.value > 0.33f) return;
        FireRandomHeadline();
    }

    public void FireRandomHeadline()
    {
        if (headlines.Count == 0) return;
        var h = headlines[Random.Range(0, headlines.Count)];

        foreach (var card in allCards)
            if (card.playerName == h.targetPlayerName)
            {
                card.currentMarketValue = Mathf.Max(0.01f,
                    Mathf.Round(card.currentMarketValue * h.priceMultiplier * 100f) / 100f);
            }

        OnHeadlineBroadcast?.Invoke(h);
        NotificationSystem.Show($"📰 {h.headline}");
        AudioManager.Play("ticker_alert");
    }
}
