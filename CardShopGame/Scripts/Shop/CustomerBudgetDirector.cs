using UnityEngine;

/// <summary>
/// Assigned to each CustomerAI at spawn. Derives wallet size from store
/// reputation and seasonal hype; higher reputation pushes more Whale spawns.
/// </summary>
public class CustomerBudgetDirector : MonoBehaviour
{
    public enum CustomerTier { CasualKid, HobbyRegular, LocalHighRoller, MysteryWhale }

    public CustomerTier  tier   { get; private set; }
    public float         budget { get; private set; }

    // ----------------------------------------------------------------

    public void Assign()
    {
        float rep  = ReputationManager.Instance ? ReputationManager.Instance.ReputationScore : 50f;
        float roll = Random.Range(0f, 100f) + (rep > 80f ? 15f : 0f);

        if (roll < 40f)
        {
            tier   = CustomerTier.CasualKid;
            budget = Random.Range(10f, 35f);
        }
        else if (roll < 80f)
        {
            tier   = CustomerTier.HobbyRegular;
            budget = Random.Range(60f, 200f);
        }
        else if (roll < 95f)
        {
            tier   = CustomerTier.LocalHighRoller;
            budget = Random.Range(250f, 850f);
        }
        else
        {
            tier   = CustomerTier.MysteryWhale;
            budget = Random.Range(1000f, 5000f);
        }

        budget = Mathf.Round(budget * 100f) / 100f;
    }

    public bool CanAfford(float price) => price <= budget;

    public void Spend(float amount)
    {
        budget = Mathf.Round(Mathf.Max(0f, budget - amount) * 100f) / 100f;
    }
}
