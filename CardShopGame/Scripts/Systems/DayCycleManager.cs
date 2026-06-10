using System;
using UnityEngine;

/// <summary>
/// Manages in-game time and shop state transitions.
/// One real-time second = configurable game minutes.
/// Fires events that all other systems subscribe to rather than polling.
/// </summary>
public class DayCycleManager : MonoBehaviour
{
    public static DayCycleManager Instance { get; private set; }

    // ----------------------------------------------------------------
    // Events

    public static event Action OnNewDay;
    public static event Action OnShopOpen;
    public static event Action OnShopClose;

    // ----------------------------------------------------------------
    // Game state

    public enum GameState { DayPreparation, ShopOpen, SummaryScreen }

    public GameState CurrentState { get; private set; } = GameState.DayPreparation;

    // ----------------------------------------------------------------
    // Settings

    [Header("Time Settings")]
    [Tooltip("How many real seconds equal one in-game hour")]
    public float secondsPerGameHour = 30f;

    [Header("Business Hours (24h clock)")]
    public int openHour  = 9;
    public int closeHour = 20;

    // ----------------------------------------------------------------
    // Read-only state

    public int   CurrentDay       { get; private set; } = 1;
    public int   CurrentDayOfWeek { get; private set; } = 0;   // 0 = Monday
    public float CurrentHour      { get; private set; } = 8f;
    public bool  ShopIsOpen       => CurrentState == GameState.ShopOpen;

    // Daily financial accumulators — reset each new day
    public float DailyRevenue  { get; private set; }
    public float DailyExpenses { get; private set; }

    // ----------------------------------------------------------------

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
        if (CurrentState != GameState.ShopOpen &&
            CurrentState != GameState.DayPreparation) return;

        _timer += Time.deltaTime;
        if (_timer >= secondsPerGameHour)
        {
            _timer -= secondsPerGameHour;
            AdvanceHour();
        }
    }

    // ----------------------------------------------------------------

    private void AdvanceHour()
    {
        CurrentHour += 1f;

        if (CurrentHour >= 24f)
        {
            CurrentHour = 0f;
            // Closing fires before midnight rolls, but we also need a new-day tick
            if (CurrentState == GameState.ShopOpen)
                TransitionToState(GameState.SummaryScreen);
        }

        bool shouldBeOpen = CurrentHour >= openHour && CurrentHour < closeHour
                            && CurrentState != GameState.SummaryScreen;

        if (shouldBeOpen && !_wasOpen)
        {
            _wasOpen = true;
            TransitionToState(GameState.ShopOpen);
        }
        else if (!shouldBeOpen && _wasOpen && CurrentState == GameState.ShopOpen)
        {
            _wasOpen = false;
            TransitionToState(GameState.SummaryScreen);
        }
    }

    // ----------------------------------------------------------------
    // Public API

    public void TransitionToState(GameState next)
    {
        if (CurrentState == next) return;
        CurrentState = next;

        switch (next)
        {
            case GameState.ShopOpen:
                OnShopOpen?.Invoke();
                break;

            case GameState.SummaryScreen:
                OnShopClose?.Invoke();
                break;
        }
    }

    /// <summary>Called by UIManager end-of-day confirm button.</summary>
    public void StartNewDay()
    {
        CurrentDay++;
        CurrentDayOfWeek = (CurrentDayOfWeek + 1) % 7;
        CurrentHour      = 8f;
        DailyRevenue     = 0f;
        DailyExpenses    = 0f;
        _wasOpen         = false;
        TransitionToState(GameState.DayPreparation);
        OnNewDay?.Invoke();
    }

    // ----------------------------------------------------------------
    // Financial accumulators

    public void RecordRevenue(float amount)
    {
        DailyRevenue = Mathf.Round((DailyRevenue + amount) * 100f) / 100f;
    }

    public void RecordExpense(float amount)
    {
        DailyExpenses = Mathf.Round((DailyExpenses + amount) * 100f) / 100f;
    }

    // ----------------------------------------------------------------
    // Save / load support

    public void LoadDayState(int day, int dayOfWeek)
    {
        CurrentDay       = Mathf.Max(1, day);
        CurrentDayOfWeek = Mathf.Clamp(dayOfWeek, 0, 6);
    }

    // ----------------------------------------------------------------

    public string GetTimeString()
    {
        int    hour = (int)CurrentHour;
        string ampm = hour >= 12 ? "PM" : "AM";
        int    h12  = hour % 12;
        if (h12 == 0) h12 = 12;
        return $"Day {CurrentDay} — {h12}:00 {ampm}";
    }
}
