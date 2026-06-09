using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central game controller — holds shop funds, tracks daily P&L,
/// and coordinates all other manager singletons.
/// </summary>
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    // ---- Economy -------------------------------------------------------
    [Header("Economy")]
    public float shopFunds = 2000f;

    // Daily P&L tracking
    public float DailyRevenue     { get; private set; }
    public float DailyWholesale   { get; private set; }
    public float DailyGradingFees { get; private set; }
    public float DailyUtilityCost => 50f + ShopUpgradeSystem.Instance.ShopLevel * 15f;

    // ---- References ----------------------------------------------------
    [Header("Scene References")]
    public Transform spawnPoint;
    public Transform exitPoint;
    public Transform checkoutTransform;

    public event Action<float> OnFundsChanged;

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        DayCycleManager.OnNewDay   += HandleNewDay;
        DayCycleManager.OnShopOpen += HandleShopOpen;
        DayCycleManager.OnShopClose += HandleShopClose;

        if (SaveSystem.SaveExists())
        {
            var data = SaveSystem.Load();
            if (data != null) shopFunds = data.shopFunds;
        }

        AudioManager.PlayMusic("shop_ambience");
    }

    private void OnApplicationQuit() => SaveSystem.Save();

    // ----------------------------------------------------------------

    public void AddFunds(float amount)
    {
        shopFunds    += amount;
        DailyRevenue += amount;
        OnFundsChanged?.Invoke(shopFunds);
    }

    public bool SpendFunds(float amount, string reason = "")
    {
        if (shopFunds < amount)
        {
            NotificationSystem.Show("Insufficient funds!");
            return false;
        }
        shopFunds -= amount;
        OnFundsChanged?.Invoke(shopFunds);
        if (!string.IsNullOrEmpty(reason)) DailyWholesale += amount;
        return true;
    }

    public void ChargeGradingFee(float fee)
    {
        shopFunds       -= fee;
        DailyGradingFees += fee;
        OnFundsChanged?.Invoke(shopFunds);
    }

    // ----------------------------------------------------------------

    private void HandleShopOpen()
    {
        AudioManager.Play("door_open");
        NotificationSystem.Show("Shop is now OPEN!");
        CustomerSpawner.Instance.StartSpawning();
    }

    private void HandleShopClose()
    {
        CustomerSpawner.Instance.StopSpawning();
        AudioManager.Play("door_close");
        // Deduct utilities
        shopFunds -= DailyUtilityCost;
        NotificationSystem.Show("Shop is CLOSED for the day.");
        DailyLedgerUI.Instance?.ShowLedger(DailyRevenue, DailyWholesale, DailyGradingFees, DailyUtilityCost);
    }

    private void HandleNewDay()
    {
        // Reset daily counters
        DailyRevenue     = 0f;
        DailyWholesale   = 0f;
        DailyGradingFees = 0f;
        SaveSystem.Save();
    }
}
