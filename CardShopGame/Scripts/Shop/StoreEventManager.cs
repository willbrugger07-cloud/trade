using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rolls a random crisis or bonus event each morning.
/// Events last one full business day and modify core systems.
/// </summary>
public class StoreEventManager : MonoBehaviour
{
    public static StoreEventManager Instance { get; private set; }

    [Serializable]
    public class ShopEvent
    {
        public string title;
        public string description;
        public EventType type;
    }

    public enum EventType
    {
        None,
        PowerOutage,
        SupplyShortage,
        TaxAudit,
        CollectorConvention,
        CardHeist,               // Shoplifter surge
        CelebrityVisit,          // Massive whale spawn
    }

    [Header("Event Pool")]
    public List<ShopEvent> eventPool;
    [Range(0f, 1f)] public float eventChance = 0.35f;

    public ShopEvent TodaysEvent { get; private set; }

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()  => DayCycleManager.OnNewDay += RollEvent;
    private void OnDisable() => DayCycleManager.OnNewDay -= RollEvent;

    // ----------------------------------------------------------------

    private void RollEvent()
    {
        TodaysEvent = null;
        if (Random.value > eventChance || eventPool.Count == 0) return;

        TodaysEvent = eventPool[Random.Range(0, eventPool.Count)];
        ApplyEvent(TodaysEvent);
        NotificationSystem.Show($"⚠️ {TodaysEvent.title}: {TodaysEvent.description}");
        AudioManager.Play("ticker_alert");
    }

    private void ApplyEvent(ShopEvent ev)
    {
        switch (ev.type)
        {
            case EventType.CollectorConvention:
                MarketingManager.Instance.HypeMultiplier += 1.0f;  // doubles foot traffic
                break;

            case EventType.CelebrityVisit:
                // spawn a guaranteed Whale customer immediately
                CustomerSpawner.Instance?.StartSpawning();
                break;

            case EventType.TaxAudit:
                float fine = ShopManager.Instance.shopFunds * 0.05f;
                ShopManager.Instance.SpendFunds(fine);
                NotificationSystem.Show($"💸 Tax audit fine: ${fine:F2}");
                break;

            case EventType.CardHeist:
                // Triple shoplifter chance for the day — handled in CustomerSpawner
                break;

            case EventType.SupplyShortage:
                // Handled by OrderTerminal — prices flagged as elevated today
                break;

            case EventType.PowerOutage:
                // Employees process 50% slower — handled by EmployeeAI cooldown
                break;
        }
    }
}
