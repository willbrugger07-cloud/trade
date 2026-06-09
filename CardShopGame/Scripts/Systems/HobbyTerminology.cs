using UnityEngine;

/// <summary>
/// Static helpers returning authentic hobby-specific labels for each sport.
/// Used in UI tooltips, notification text, and customer dialogue.
/// </summary>
public static class HobbyTerminology
{
    public static string GetRookieLabel(SportsCard.CardSport sport) =>
        sport switch
        {
            SportsCard.CardSport.Baseball => "1st Bowman Chrome Prospect",
            SportsCard.CardSport.Hockey   => "Young Guns Rookie",
            _                             => "Rookie Card (RC)",
        };

    public static string GetCaseHitName(SportsCard.CardSport sport) =>
        sport switch
        {
            SportsCard.CardSport.Basketball => "Downtown Insert",
            SportsCard.CardSport.Football   => "Kaboom! Glow Parallel",
            SportsCard.CardSport.Baseball   => "Heavy Lumber / Home Run Challenge",
            SportsCard.CardSport.Hockey     => "Clear Cut / Exquisite Patch",
            _                               => "Ultra-Rare Case Hit",
        };

    public static string GetSeasonLabel(SportsSeasonManager.SportsSeason season) =>
        season switch
        {
            SportsSeasonManager.SportsSeason.Basketball_Playoffs => "NBA Playoffs Season",
            SportsSeasonManager.SportsSeason.Football_Kickoff    => "NFL Kickoff Season",
            SportsSeasonManager.SportsSeason.Baseball_Summer     => "MLB Summer Season",
            SportsSeasonManager.SportsSeason.Hockey_Winter       => "NHL Winter Season",
            _                                                    => "Off-Season",
        };

    /// <summary>
    /// Friendly flavour tip shown in the order terminal so the player knows
    /// which boxes are cheap to buy now and which to sit on for profit.
    /// </summary>
    public static string GetSeasonBuyTip(SportsCard.CardSport sport)
    {
        if (SportsSeasonManager.Instance == null) return "";

        float mult = SportsSeasonManager.Instance.GetDemandMultiplier(sport);
        if (mult >= 1.40f) return "🔥 PEAK SEASON — sell now for maximum profit!";
        if (mult >= 1.10f) return "📈 Demand elevated — good time to open product.";
        if (mult <= 0.80f) return "💡 Off-season — stock up at discount for later.";
        return "📊 Stable demand.";
    }
}
