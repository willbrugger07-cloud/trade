using UnityEngine;

/// <summary>
/// Tracks the shop's public reputation (0–100).
/// High reputation speeds up customer spawning and unlocks Whale archetype.
/// Low reputation slows traffic and can trigger negative press events.
/// </summary>
public class ReputationManager : MonoBehaviour
{
    public static ReputationManager Instance { get; private set; }

    [Range(0f, 100f)] public float Reputation { get; private set; } = 50f;

    [Header("Penalty Thresholds")]
    [Range(1f, 2f)] public float gougeThreshold = 1.35f;  // >35% markup hurts rep

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ----------------------------------------------------------------

    /// <summary>Call whenever a card sells. Penalizes excessive markups.</summary>
    public void RecordSale(float retailPrice, float marketValue)
    {
        float ratio = marketValue > 0 ? retailPrice / marketValue : 1f;
        if (ratio > gougeThreshold)
        {
            Modify(-2.5f);
            NotificationSystem.Show("📉 Customer complained about the price!");
        }
        else if (ratio <= 1.0f)
        {
            Modify(0.75f);
        }
    }

    public void RecordCounterfeitSold()
    {
        Modify(-15f);
        NotificationSystem.Show("🚨 Fake card sold! Reputation took a huge hit.");
    }

    public void RecordPositiveEvent(float bonus = 2f) => Modify(bonus);

    public void Modify(float delta)
    {
        Reputation = Mathf.Clamp(Reputation + delta, 0f, 100f);
        ShopHUD.Instance?.RefreshReputation(Reputation);
    }

    public float SpawnIntervalModifier()
    {
        // Maps 0–100 rep to a 0.5×–2× spawn speed multiplier
        return Mathf.Lerp(2f, 0.5f, Reputation / 100f);
    }
}
