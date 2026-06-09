using UnityEngine;

/// <summary>
/// Insurance tier system and physical vault door control.
/// Deducts daily premium; pays out on theft losses based on coverage %.
/// </summary>
public class SecurityVaultManager : MonoBehaviour
{
    public static SecurityVaultManager Instance { get; private set; }

    public enum InsuranceTier { None, Silver, Gold }

    [Header("Insurance")]
    public InsuranceTier activePolicy = InsuranceTier.None;
    public float premiumMonthlyCost      { get; private set; }
    public float payoutCoveragePercent   { get; private set; }

    [Header("Vault Hardware")]
    public bool      isVaultLocked = true;
    public GameObject laserGridPrefab;

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()  => DayCycleManager.OnShopClose += ChargeDaily;
    private void OnDisable() => DayCycleManager.OnShopClose -= ChargeDaily;

    // ----------------------------------------------------------------

    public void PurchaseInsurance(InsuranceTier tier)
    {
        activePolicy = tier;
        switch (tier)
        {
            case InsuranceTier.None:
                payoutCoveragePercent = 0f;
                premiumMonthlyCost    = 0f;
                break;
            case InsuranceTier.Silver:
                payoutCoveragePercent = 0.60f;
                premiumMonthlyCost    = 45f;
                break;
            case InsuranceTier.Gold:
                payoutCoveragePercent = 0.95f;
                premiumMonthlyCost    = 120f;
                break;
        }
        NotificationSystem.Show($"🔒 Insurance updated to {tier}. Monthly cost: ${premiumMonthlyCost:F2}");
    }

    public void SetVaultLock(bool locked)
    {
        isVaultLocked = locked;
        if (laserGridPrefab) laserGridPrefab.SetActive(locked);
        NotificationSystem.Show(locked ? "Vault secured." : "Vault open.");
        AudioManager.Play(locked ? "vault_lock" : "vault_unlock");
    }

    public void ProcessTheftClaim(CardInstance stolen)
    {
        float loss    = stolen.GetSalePrice();
        float payout  = Mathf.Round(loss * payoutCoveragePercent * 100f) / 100f;
        if (payout > 0f)
        {
            ShopManager.Instance.AddFunds(payout);
            NotificationSystem.Show($"💰 Insurance payout: ${payout:F2} for stolen {stolen.data.cardName}");
        }
    }

    private void ChargeDaily()
    {
        if (premiumMonthlyCost <= 0f) return;
        float daily = Mathf.Round(premiumMonthlyCost / 30f * 100f) / 100f;
        ShopManager.Instance.SpendFunds(daily);
    }
}
