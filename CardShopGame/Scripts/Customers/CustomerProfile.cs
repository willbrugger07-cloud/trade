using UnityEngine;

public enum CollectorArchetype
{
    NostalgiaGuy,   // vintage sets, low budget
    Gambler,        // only buys packs to rip in-store
    Flipper,        // hunts mispriced cards below market value
    Investor,       // Whale — graded slabs only, huge budget
}

/// <summary>
/// Attach alongside CustomerAI. Generates a personality profile on Start
/// and exposes it to the behaviour tree for preference-based browsing.
/// </summary>
public class CustomerProfile : MonoBehaviour
{
    public CollectorArchetype Archetype       { get; private set; }
    public float              ShoppingBudget  { get; private set; }
    public string             PreferredSeries { get; private set; }
    public float              PatienceLevel   { get; private set; }

    private void Start() => GenerateProfile();

    private void GenerateProfile()
    {
        float roll = Random.Range(0f, 100f);

        if (roll < 40f)
        {
            Archetype      = CollectorArchetype.NostalgiaGuy;
            ShoppingBudget = Random.Range(20f, 75f);
            PreferredSeries = "Vintage Topps";
            PatienceLevel  = 0.8f;
        }
        else if (roll < 75f)
        {
            Archetype      = CollectorArchetype.Gambler;
            ShoppingBudget = Random.Range(100f, 300f);
            PreferredSeries = "Select Booster Packs";
            PatienceLevel  = 1.2f;

            // Override CustomerAI archetype to Ripper
            var ai = GetComponent<CustomerAI>();
            if (ai) ai.type = CustomerType.Ripper;
        }
        else if (roll < 93f)
        {
            Archetype      = CollectorArchetype.Flipper;
            ShoppingBudget = Random.Range(150f, 500f);
            PreferredSeries = "Any (below market)";
            PatienceLevel  = 1.0f;

            var ai = GetComponent<CustomerAI>();
            if (ai) ai.type = CustomerType.BargainHunter;
        }
        else
        {
            Archetype      = CollectorArchetype.Investor;
            ShoppingBudget = Random.Range(800f, 5000f);
            PreferredSeries = "PSA 10 Graded";
            PatienceLevel  = 0.6f;

            var ai = GetComponent<CustomerAI>();
            if (ai) { ai.type = CustomerType.Whale; ai.budget = ShoppingBudget; }
        }
    }
}
