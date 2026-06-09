using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Commercial property & sub-let system.
/// Player starts in a small leased unit; profits let them buy/move to larger spaces.
/// Sub-letting spare rooms to third-party vendors generates passive daily income.
/// </summary>
public class RealEstateManager : MonoBehaviour
{
    public static RealEstateManager Instance { get; private set; }

    [Header("Property Catalog")]
    public List<Property> catalog;
    public int            activeIndex = 0;

    [Header("Sub-Let")]
    public float monthlySubLetIncome { get; private set; }
    public int   subLetCount         { get; private set; }

    [Serializable]
    public class Property
    {
        public string    name;
        public int       squareFt;
        public float     monthlyRent;
        public float     purchasePrice;
        public bool      owned;
        public GameObject sceneChunk;   // modular room prefab to enable
    }

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()  => DayCycleManager.OnShopClose += ProcessDailyFinances;
    private void OnDisable() => DayCycleManager.OnShopClose -= ProcessDailyFinances;

    // ----------------------------------------------------------------

    public Property Current => catalog[activeIndex];

    public bool TryPurchaseProperty(int index)
    {
        if (index < 0 || index >= catalog.Count) return false;
        var prop = catalog[index];

        if (prop.owned) { NotificationSystem.Show("Already owned."); return false; }
        if (!ShopManager.Instance.SpendFunds(prop.purchasePrice, "real estate")) return false;

        prop.owned = true;
        NotificationSystem.Show($"🏢 Purchased {prop.name}!");
        return true;
    }

    public bool TryRelocate(int index)
    {
        if (index < 0 || index >= catalog.Count) return false;
        var next = catalog[index];
        if (!next.owned && next.monthlyRent > 0)
        {
            // Leased — just move in, no purchase needed
        }
        else if (!next.owned)
        {
            NotificationSystem.Show("You don't own this property yet.");
            return false;
        }

        catalog[activeIndex].sceneChunk?.SetActive(false);
        next.sceneChunk?.SetActive(true);
        activeIndex = index;
        NotificationSystem.Show($"Moved to {next.name} ({next.squareFt} sq ft).");
        AudioManager.Play("upgrade_fanfare");
        return true;
    }

    public void AddSubLet(float monthlyPayout)
    {
        subLetCount++;
        monthlySubLetIncome += monthlyPayout;
        NotificationSystem.Show($"📑 Sub-let signed — +${monthlyPayout/30f:F2}/day passive.");
    }

    private void ProcessDailyFinances()
    {
        var prop = Current;

        if (!prop.owned && prop.monthlyRent > 0)
        {
            float daily = prop.monthlyRent / 30f;
            ShopManager.Instance.SpendFunds(daily);
        }

        if (monthlySubLetIncome > 0)
        {
            float passiveDaily = monthlySubLetIncome / 30f;
            ShopManager.Instance.AddFunds(passiveDaily);
        }
    }
}
