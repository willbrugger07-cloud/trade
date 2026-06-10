using UnityEngine;

/// <summary>
/// Tracks the shop's public reputation (0–100).
/// High reputation speeds up customer spawning and unlocks Whale archetype.
/// Low reputation slows traffic and can trigger negative press events.
/// </summary>
public class ReputationManager : MonoBehaviour
{
    public static ReputationManager Instance { get; private set; }

    [Range(0f, 100f)] public float startingReputation = 50f;
    [Range(1f, 2f)]   public float gougeThreshold     = 1.35f;

    public float ReputationScore { get; private set; }

    // Legacy alias so older scripts that reference .Reputation still compile
    public float Reputation => ReputationScore;

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        ReputationScore = startingReputation;
    }

    // ----------------------------------------------------------------

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

    public void RecordPositiveEvent(float bonus = 2f)  => Modify(bonus);
    public void RecordNegativeEvent(float penalty = 2f) => Modify(-Mathf.Abs(penalty));

    public void SetScore(float value)
    {
        ReputationScore = Mathf.Clamp(value, 0f, 100f);
        UIManager.Instance?.RefreshReputation(ReputationScore);
    }

    public void Modify(float delta)
    {
        ReputationScore = Mathf.Clamp(ReputationScore + delta, 0f, 100f);
        UIManager.Instance?.RefreshReputation(ReputationScore);
    }

    public float SpawnIntervalModifier() =>
        Mathf.Lerp(2f, 0.5f, ReputationScore / 100f);

    /// <summary>Rep so low no new customers will enter voluntarily.</summary>
    public bool IsDeadStore() => ReputationScore < 20f;
}
