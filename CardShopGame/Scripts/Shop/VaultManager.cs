using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Off-site secure vault for long-term card investment.
/// Vaulted cards slowly appreciate (or dip). Weekly maintenance fee applies.
/// Accessible from the Tablet or a dedicated in-shop terminal.
/// </summary>
public class VaultManager : MonoBehaviour
{
    public static VaultManager Instance { get; private set; }

    [Header("Vault Costs")]
    public float maintenanceFeePerCardPerDay = 2f;

    private readonly List<VaultEntry> _vault = new();

    public IReadOnlyList<VaultEntry> Vault => _vault;

    [Serializable]
    public class VaultEntry
    {
        public CardInstance card;
        public float        valueAtDeposit;
        public int          daysInVault;
    }

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()  => DayCycleManager.OnNewDay += ProcessDay;
    private void OnDisable() => DayCycleManager.OnNewDay -= ProcessDay;

    // ----------------------------------------------------------------

    public bool Deposit(CardInstance card)
    {
        InventoryManager.Instance.RemoveFromBackRoom(card);
        _vault.Add(new VaultEntry
        {
            card           = card,
            valueAtDeposit = card.GetSalePrice(),
            daysInVault    = 0,
        });
        NotificationSystem.Show($"🔒 {card.GetDisplayName()} locked in the vault.");
        return true;
    }

    public void Withdraw(VaultEntry entry)
    {
        _vault.Remove(entry);
        InventoryManager.Instance.AddToBackRoom(entry.card);
        float gain = entry.card.GetSalePrice() - entry.valueAtDeposit;
        string delta = gain >= 0 ? $"+${gain:F2}" : $"-${Mathf.Abs(gain):F2}";
        NotificationSystem.Show($"📤 Withdrew {entry.card.GetDisplayName()} ({delta} since deposit)");
    }

    private void ProcessDay()
    {
        float fee = _vault.Count * maintenanceFeePerCardPerDay;
        if (fee > 0)
        {
            ShopManager.Instance.SpendFunds(fee);
            NotificationSystem.Show($"💰 Vault maintenance: ${fee:F2}");
        }

        // Vaulted cards appreciate slowly (rare cards held long-term surge occasionally)
        foreach (var entry in _vault)
        {
            entry.daysInVault++;
            float drift = UnityEngine.Random.Range(-0.02f, 0.06f);
            if (entry.daysInVault % 7 == 0 && UnityEngine.Random.value < 0.15f) drift += 0.10f;
            entry.card.data.currentMarketValue = Mathf.Max(0.01f,
                entry.card.data.currentMarketValue * (1f + drift));
        }
    }
}
