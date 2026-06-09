using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines a single product configuration tier (Hanger, Blaster, Hobby, etc.)
/// for a parent card series. Drive the OrderTerminal catalog from a list of these.
/// </summary>
[CreateAssetMenu(fileName = "NewProductConfig", menuName = "Sports Card Tycoon/Product Configuration")]
public class ProductConfigData : ScriptableObject
{
    public enum PackFormat { HangerBox, BlasterBox, MegaBox, HobbyBox, BreakersDelight }

    [Header("Identity")]
    public string     parentSeriesName;
    public PackFormat boxFormat;

    [Header("Economy")]
    public float wholesaleCost;
    public float suggestedRetailPrice;

    [Header("Physical")]
    public Vector3 shelfBoxDimensions = new Vector3(0.15f, 0.10f, 0.25f);

    [Header("Pack Contents")]
    public int totalPacksInBox = 1;
    public int cardsPerPack    = 4;

    [Header("Guaranteed Hits (Hobby & Breaker tiers)")]
    public int guaranteedAutographs          = 0;
    public int guaranteedMemorabiliaPatches  = 0;

    [Header("Exclusive Retail Parallel")]
    public string exclusiveParallelName;
    [Range(0f, 1f)] public float exclusiveParallelChance;

    // ----------------------------------------------------------------

    /// <summary>Multiplier applied to customer interest based on box tier.</summary>
    public float GetCollectorAppealMultiplier() =>
        boxFormat switch
        {
            PackFormat.HangerBox      => 1.00f,
            PackFormat.BlasterBox     => 1.10f,
            PackFormat.MegaBox        => 1.15f,
            PackFormat.HobbyBox       => 1.50f,
            PackFormat.BreakersDelight => 2.50f,
            _                         => 1.00f,
        };

    /// <summary>Which customer tier will willingly buy this format.</summary>
    public CustomerBudgetDirector.CustomerTier MinimumTier() =>
        boxFormat switch
        {
            PackFormat.HobbyBox        => CustomerBudgetDirector.CustomerTier.LocalHighRoller,
            PackFormat.BreakersDelight => CustomerBudgetDirector.CustomerTier.MysteryWhale,
            _                          => CustomerBudgetDirector.CustomerTier.CasualKid,
        };
}
