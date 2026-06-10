using UnityEngine;

/// <summary>
/// Tracks the active sports season and provides demand multipliers.
/// Seasons rotate every 15 in-game days, shifting customer spending priorities.
/// </summary>
public class SportsSeasonManager : MonoBehaviour
{
    public static SportsSeasonManager Instance { get; private set; }

    public enum SportsSeason { Basketball_Playoffs, Football_Kickoff, Baseball_Summer, Hockey_Winter }

    public SportsSeason ActiveSeason { get; private set; } = SportsSeason.Football_Kickoff;

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()  => DayCycleManager.OnNewDay += CheckSeasonProgression;
    private void OnDisable() => DayCycleManager.OnNewDay -= CheckSeasonProgression;

    // ----------------------------------------------------------------

    private int _lastSeasonDay;

    private void CheckSeasonProgression()
    {
        int day     = DayCycleManager.Instance.CurrentDay;
        int daysIn  = (day - _lastSeasonDay);
        int daysLeft = 15 - (daysIn % 15);

        // Countdown warning 3 days before shift
        if (daysLeft <= 3 && daysLeft > 0)
        {
            var nextSeason = (SportsSeason)(((int)ActiveSeason + 1) % 4);
            string nextLabel = nextSeason.ToString().Replace('_', ' ');
            UIManager.Instance?.PushTickerLine(
                $"⚠ SEASON CHANGE IN {daysLeft} DAY{(daysLeft == 1 ? "" : "S")} → {nextLabel}");
        }

        if (day % 15 != 0) return;

        _lastSeasonDay = day;
        ActiveSeason   = (SportsSeason)(((int)ActiveSeason + 1) % 4);

        string label = ActiveSeason.ToString().Replace('_', ' ');
        NotificationSystem.Show($"📅 Season shift: {label}! Adjust your inventory strategy.");
        AudioManager.Play("ticker_alert");
        UIManager.Instance?.PushTickerLine($"🔄 SEASON SHIFT → {label}");
    }

    /// <summary>
    /// Returns the demand multiplier for a given sport in the current season.
    /// Values above 1 mean customers pay more and buy faster; below 1 they resist.
    /// </summary>
    public float GetDemandMultiplier(CardSport sport)
    {
        return ActiveSeason switch
        {
            SportsSeason.Basketball_Playoffs => sport switch
            {
                CardSport.Basketball => 1.50f,
                CardSport.Baseball   => 0.80f,
                _                               => 1.00f,
            },
            SportsSeason.Football_Kickoff => sport switch
            {
                CardSport.Football   => 1.60f,
                CardSport.Hockey     => 0.85f,
                _                               => 1.00f,
            },
            SportsSeason.Baseball_Summer => sport switch
            {
                CardSport.Baseball   => 1.45f,
                CardSport.Football   => 0.75f,
                _                               => 1.00f,
            },
            SportsSeason.Hockey_Winter => sport switch
            {
                CardSport.Hockey     => 1.55f,
                CardSport.Basketball => 1.10f,
                _                               => 1.00f,
            },
            _ => 1.00f,
        };
    }

    /// <summary>Adjusted market price factoring in seasonal hype.</summary>
    public float GetSeasonalValue(CardInstance card)
    {
        float base_ = card.GetSalePrice();
        float mult  = GetDemandMultiplier(card.data.sport);
        return Mathf.Round(base_ * mult * 100f) / 100f;
    }
}
