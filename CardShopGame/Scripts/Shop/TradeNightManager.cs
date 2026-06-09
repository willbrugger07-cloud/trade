using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages scheduled Trade Night events (Friday evenings by default).
/// Spawns a wave of trade-binder customers, boosts hype, and awards a
/// reputation bonus at the end based on successful trades completed.
/// </summary>
public class TradeNightManager : MonoBehaviour
{
    public static TradeNightManager Instance { get; private set; }

    [Header("Schedule")]
    public int tradeNightDayOfWeek = 5;   // 0=Monday … 6=Sunday

    [Header("Wave Settings")]
    public int minTraders  = 4;
    public int maxTraders  = 10;
    public float spawnIntervalSecs = 45f;

    [Header("State")]
    public bool IsActive { get; private set; }
    public int  TradesCompleted { get; private set; }

    private Coroutine _wave;

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()  => DayCycleManager.OnNewDay += CheckSchedule;
    private void OnDisable() => DayCycleManager.OnNewDay -= CheckSchedule;

    // ----------------------------------------------------------------

    private void CheckSchedule()
    {
        if (DayCycleManager.Instance.CurrentDayOfWeek == tradeNightDayOfWeek)
            BeginTradeNight();
    }

    public void BeginTradeNight()
    {
        if (IsActive) return;
        IsActive = true;
        TradesCompleted = 0;

        MarketingManager.Instance.HypeMultiplier += 0.5f;
        NotificationSystem.Show("🌙 TRADE NIGHT BEGINS! Collectors are flooding in with binders!");
        AudioManager.Play("crowd_cheer");

        _wave = StartCoroutine(SpawnWave());
    }

    private IEnumerator SpawnWave()
    {
        int count = Random.Range(minTraders, maxTraders + 1);
        for (int i = 0; i < count; i++)
        {
            CustomerSpawner.Instance?.SpawnTradeCustomer();
            yield return new WaitForSeconds(spawnIntervalSecs);
        }
    }

    /// <summary>Called by TradingBinderDesk on each successful trade.</summary>
    public void RecordSuccessfulTrade()
    {
        TradesCompleted++;
    }

    public void EndTradeNight()
    {
        if (!IsActive) return;
        if (_wave != null) StopCoroutine(_wave);
        IsActive = false;

        float repBonus = TradesCompleted * 1.5f;
        ReputationManager.Instance?.RecordPositiveEvent(repBonus);
        MarketingManager.Instance.HypeMultiplier -= 0.5f;

        NotificationSystem.Show($"🌙 Trade Night over. {TradesCompleted} trades completed. +{repBonus:F0} rep!");
        AudioManager.Play("end_of_day_bell");
    }
}
