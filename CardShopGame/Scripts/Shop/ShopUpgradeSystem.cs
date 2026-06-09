using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tiered shop upgrade system. Each level unlocks new furniture, display space,
/// and faster customer spawn rates. Upgrades cost increasing amounts of in-game cash.
/// </summary>
public class ShopUpgradeSystem : MonoBehaviour
{
    public static ShopUpgradeSystem Instance { get; private set; }

    public int ShopLevel { get; private set; } = 1;

    [Serializable]
    public struct Upgrade
    {
        public string    name;
        public string    description;
        public float     cost;
        public GameObject[] objectsToEnable;   // Furniture / shelves unlocked
    }

    public List<Upgrade> upgradeTiers;

    public event Action<int> OnLevelUp;

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool CanUpgrade()      => ShopLevel < upgradeTiers.Count;
    public float NextUpgradeCost  => CanUpgrade() ? upgradeTiers[ShopLevel - 1].cost : float.MaxValue;

    public bool TryUpgrade()
    {
        if (!CanUpgrade())
        {
            NotificationSystem.Show("Shop is already at maximum level!");
            return false;
        }

        var upgrade = upgradeTiers[ShopLevel - 1];
        if (!ShopManager.Instance.SpendFunds(upgrade.cost, "upgrade"))
            return false;

        foreach (var obj in upgrade.objectsToEnable)
            if (obj) obj.SetActive(true);

        ShopLevel++;
        OnLevelUp?.Invoke(ShopLevel);
        NotificationSystem.Show($"⬆️ Shop upgraded to Level {ShopLevel}! {upgrade.name} unlocked.");
        AudioManager.Play("upgrade_fanfare");
        return true;
    }
}
