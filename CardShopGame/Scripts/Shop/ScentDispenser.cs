using UnityEngine;

/// <summary>
/// Wall-mounted aerosol dispenser. Different scent cartridges alter
/// the behavior stats of AI customers currently on the shop floor.
/// </summary>
public class ScentDispenser : MonoBehaviour, IInteractable
{
    public enum ScentType { None, VintageBubblegum, FactoryFoil, PremiumLeather }

    public ScentType activeScent = ScentType.None;
    public float cartridgeLevel  = 100f;
    public float consumptionRate = 0.05f;   // units/second while shop is open

    // ----------------------------------------------------------------

    public void Interact(PlayerInteraction player) =>
        ScentDispenserUI.Instance?.Open(this);

    private void Update()
    {
        if (activeScent == ScentType.None) return;
        if (DayCycleManager.Instance?.CurrentState != DayCycleManager.GameState.ShopOpen) return;

        cartridgeLevel -= consumptionRate * Time.deltaTime;
        if (cartridgeLevel <= 0f)
        {
            cartridgeLevel = 0f;
            activeScent    = ScentType.None;
            NotificationSystem.Show("Scent cartridge empty — atmosphere back to baseline.");
        }
    }

    public void LoadCartridge(ScentType type)
    {
        activeScent    = type;
        cartridgeLevel = 100f;
        ApplyModifiers();
        NotificationSystem.Show($"🌫 Scent loaded: {type}. AI behaviour updated.");
        AudioManager.Play("aerosol_spray");
    }

    private void ApplyModifiers()
    {
        foreach (var customer in FindObjectsOfType<CustomerAI>())
        {
            var profile = customer.GetComponent<CustomerProfile>();
            if (profile == null) continue;

            switch (activeScent)
            {
                case ScentType.VintageBubblegum:
                    if (profile.personality == CustomerProfile.CollectorArchetype.TheNostalgiaGuy)
                        profile.patienceLevel = Mathf.Min(profile.patienceLevel * 1.4f, 3f);
                    break;

                case ScentType.FactoryFoil:
                    if (profile.personality == CustomerProfile.CollectorArchetype.TheGambler)
                        profile.shoppingBudget *= 1.25f;
                    break;

                case ScentType.PremiumLeather:
                    if (profile.personality == CustomerProfile.CollectorArchetype.TheInvestor)
                        if (ReputationManager.Instance)
                            ReputationManager.Instance.gougeThreshold = 1.50f;
                    break;
            }
        }
    }
}
