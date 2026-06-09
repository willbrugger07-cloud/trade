using System;
using UnityEngine;

/// <summary>
/// Manages in-game time. One real-time second = configurable game minutes.
/// Fires OnNewDay at midnight and OnShopOpen/OnShopClose at business hours.
/// </summary>
public class DayCycleManager : MonoBehaviour
{
    public static DayCycleManager Instance { get; private set; }

    // Events other systems subscribe to
    public static event Action OnNewDay;
    public static event Action OnShopOpen;
    public static event Action OnShopClose;

    [Header("Time Settings")]
    [Tooltip("How many real seconds equal one in-game hour")]
    public float secondsPerGameHour = 30f;

    [Header("Business Hours (24h clock)")]
    public int openHour  = 9;
    public int closeHour = 20;

    // Read-only public state
    public int   CurrentDay  { get; private set; } = 1;
    public float CurrentHour { get; private set; } = 8f;   // start before opening
    public bool  ShopIsOpen  { get; private set; }

    private float _timer;
    private bool  _wasOpen;

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= secondsPerGameHour)
        {
            _timer -= secondsPerGameHour;
            AdvanceHour();
        }
    }

    private void AdvanceHour()
    {
        CurrentHour += 1f;

        if (CurrentHour >= 24f)
        {
            CurrentHour = 0f;
            CurrentDay++;
            OnNewDay?.Invoke();
        }

        bool open = CurrentHour >= openHour && CurrentHour < closeHour;
        if (open != _wasOpen)
        {
            ShopIsOpen = open;
            _wasOpen   = open;
            if (open) OnShopOpen?.Invoke();
            else      OnShopClose?.Invoke();
        }
    }

    /// <summary>Display string e.g. "Day 12 — 2:30 PM"</summary>
    public string GetTimeString()
    {
        int hour = (int)CurrentHour;
        string ampm = hour >= 12 ? "PM" : "AM";
        int h12  = hour % 12; if (h12 == 0) h12 = 12;
        return $"Day {CurrentDay} — {h12}:00 {ampm}";
    }
}
