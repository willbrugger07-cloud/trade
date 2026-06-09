using UnityEngine;

public enum CardSport  { Basketball, Football, Soccer, Baseball, Hockey }
public enum CardTier   { Base, Numbered, Insert, Kaboom, Downtown, Autograph, PatchAuto, Immortal }
public enum CardRarity { Common, Uncommon, Rare, UltraRare, Legendary }

[CreateAssetMenu(fileName = "New Sports Card", menuName = "Card Shop/Sports Card")]
public class SportsCard : ScriptableObject
{
    [Header("Identity")]
    public string cardName;
    public string playerName;
    public string series;        // e.g., "Prizm", "Topps Chrome"
    public int    year;
    public CardSport  sport;
    public CardTier   tier;
    public CardRarity rarity;
    public Sprite cardFrontSprite;

    [Header("Serial Numbering")]
    public bool isNumbered;
    public int  printRun;        // /99, /25, /10, /1

    [Header("Economics")]
    public float wholesalePrice;
    public float baseMarketValue;

    [Header("Pull Rate (used in pack drop tables)")]
    [Range(0f, 100f)] public float pullChance;

    [Header("Stats")]
    [Range(75, 99)] public int overallRating;

    // Modified at runtime by MarketManager each day — never serialized to disk
    [System.NonSerialized] public float currentMarketValue;

    private void OnEnable() => currentMarketValue = baseMarketValue;

    /// <summary>Returns the sale price scaled to a PSA grade.</summary>
    public float GetGradedValue(float grade)
    {
        if (grade >= 10f) return currentMarketValue * 10f;
        if (grade >= 9.5f) return currentMarketValue * 5f;
        if (grade >= 9f)  return currentMarketValue * 3f;
        if (grade >= 8f)  return currentMarketValue * 1.5f;
        return currentMarketValue;
    }
}
