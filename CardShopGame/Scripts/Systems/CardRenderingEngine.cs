using System.Collections.Generic;
using TMPro;
using UnityEngine;
using SportsCardSimulator;

/// <summary>
/// Two responsibilities:
///   1. Procedural pack drop spawner — generates GeneratedSportsCard arrays
///      from a ProductConfigData, enforcing serial-number uniqueness and
///      triggering high-intensity shader flags on 1-of-1 / SSP pulls.
///   2. Dynamic material setter — updates every visible element on the 3D
///      card prefab (TextMeshPro text, autograph overlay, foil tint, glow
///      emission) to match a supplied GeneratedSportsCard instance.
/// </summary>
public class CardRenderingEngine : MonoBehaviour
{
    public static CardRenderingEngine Instance { get; private set; }

    // ----------------------------------------------------------------
    // Inspector references for the in-world card prefab

    [Header("3D Card Prefab References")]
    public Renderer    cardFaceRenderer;
    public GameObject  autographOverlayObject;
    public Renderer    autographRenderer;
    public TextMeshPro cardNameTMP;
    public TextMeshPro serialStampTMP;
    public TextMeshPro subsetLabelTMP;
    public TextMeshPro inkColorTMP;

    [Header("Emission Settings")]
    public float baseGlowIntensity    = 1.2f;
    public float superShortGlowBoost  = 3.5f;
    public float oneOfOneGlowBoost    = 7.0f;

    // ----------------------------------------------------------------

    private readonly HashSet<string> _usedSerials = new();

    private static readonly string[] ParallelNames =
        { "Silver Refractor", "Gold Shimmer", "Zebra Stripe", "Mojo Wave", "Optic Laser" };

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ================================================================
    // 1. PROCEDURAL PACK DROP SPAWNER
    // ================================================================

    /// <summary>
    /// Generates cardsPerPack GeneratedSportsCard instances for the given
    /// box format.  Guaranteed hits are placed in the first slots for
    /// HobbyBox; BreakersDelight fills every slot with premium hits.
    /// </summary>
    public List<GeneratedSportsCard> SpawnPackDrop(ProductConfigData config,
                                                   List<string> athleteNamePool,
                                                   List<string> teamPool,
                                                   int rosterYear)
    {
        var results   = new List<GeneratedSportsCard>();
        bool isHobby  = config.boxFormat == ProductConfigData.PackFormat.HobbyBox;
        bool isBreaker = config.boxFormat == ProductConfigData.PackFormat.BreakersDelight;

        if (isBreaker)
        {
            for (int i = 0; i < config.cardsPerPack; i++)
                results.Add(BuildPremiumHit(athleteNamePool, teamPool, rosterYear, forceOnCard: true));
            return results;
        }

        int guaranteedHits = config.guaranteedAutographs + config.guaranteedMemorabiliaPatches;
        for (int i = 0; i < guaranteedHits && results.Count < config.cardsPerPack; i++)
            results.Add(BuildPremiumHit(athleteNamePool, teamPool, rosterYear, isHobby));

        while (results.Count < config.cardsPerPack)
        {
            float r = Random.value;

            if (r < config.exclusiveParallelChance && !string.IsNullOrEmpty(config.exclusiveParallelName))
                results.Add(BuildExclusiveParallel(athleteNamePool, teamPool, rosterYear,
                                                   config.exclusiveParallelName));
            else if (r < 0.02f)
                results.Add(BuildPremiumHit(athleteNamePool, teamPool, rosterYear, false));
            else
                results.Add(BuildBaseCard(athleteNamePool, teamPool, rosterYear));
        }

        return results;
    }

    // ----------------------------------------------------------------

    private GeneratedSportsCard BuildBaseCard(List<string> names, List<string> teams, int year)
    {
        var card = new GeneratedSportsCard
        {
            athleteName        = PickRandom(names),
            teamCity           = PickRandom(teams),
            rosterYear         = year,
            subsetLabel        = "Base",
            conditionRawScore  = Random.Range(7, 11),
        };
        card.visualProfile.printFinish = HobbyDataTiers.CardPrintStyle.TraditionalPaper;
        card.visualProfile.primaryFoilTint = Color.white;
        return card;
    }

    private GeneratedSportsCard BuildExclusiveParallel(List<string> names, List<string> teams,
                                                       int year, string parallelName)
    {
        var card = BuildBaseCard(names, teams, year);
        card.visualProfile.parallelName   = parallelName;
        card.visualProfile.printFinish    = HobbyDataTiers.CardPrintStyle.ChromiumFoil;
        card.visualProfile.primaryFoilTint = new Color(1f, 0.5f, 0f);
        card.visualProfile.holographicIntensity = 0.4f;

        int serial = AllocateSerial(99);
        card.visualProfile.serialCurrentIndex = serial;
        card.visualProfile.serialMaxPrintRun  = 99;
        card.subsetLabel = $"{parallelName} /99";
        return card;
    }

    private GeneratedSportsCard BuildPremiumHit(List<string> names, List<string> teams,
                                                int year, bool forceOnCard)
    {
        var card = BuildBaseCard(names, teams, year);

        bool isOneOfOne = Random.value > 0.97f;
        bool isSSP      = !isOneOfOne && Random.value > 0.85f;

        int maxRun = isOneOfOne ? 1 : isSSP ? 5 : Random.value > 0.5f ? 25 : 99;
        int serial = AllocateSerial(maxRun);

        card.visualProfile.serialCurrentIndex  = serial;
        card.visualProfile.serialMaxPrintRun   = maxRun;
        card.visualProfile.holographicIntensity = isOneOfOne ? 1.0f : isSSP ? 0.7f : 0.4f;

        if (isOneOfOne)
        {
            card.visualProfile.hitType        = HobbyDataTiers.ChaseCategory.ShieldLogoman;
            card.visualProfile.printFinish    = HobbyDataTiers.CardPrintStyle.FlawlessGilded;
            card.visualProfile.primaryFoilTint = Color.yellow;
            card.visualProfile.autoInkColor   = "1-of-1 Gold Ink";
            card.visualProfile.isDieCutShape  = true;
            card.subsetLabel = "Shield Logoman 1/1";
        }
        else
        {
            var hitType = forceOnCard
                ? HobbyDataTiers.ChaseCategory.OnCardInk
                : (Random.value > 0.5f
                    ? HobbyDataTiers.ChaseCategory.StickerAuto
                    : HobbyDataTiers.ChaseCategory.PrimePatch);

            card.visualProfile.hitType       = hitType;
            card.visualProfile.printFinish   = HobbyDataTiers.CardPrintStyle.ChromiumFoil;
            card.visualProfile.primaryFoilTint = isSSP
                ? new Color(0.2f, 1f, 0.8f)
                : new Color(0.5f, 0.5f, 1f);
            card.visualProfile.parallelName  = ParallelNames[Random.Range(0, ParallelNames.Length)];
            card.visualProfile.autoInkColor  = hitType == HobbyDataTiers.ChaseCategory.OnCardInk
                ? "Blue Ink"
                : "None";
            card.subsetLabel = $"{card.visualProfile.parallelName} {hitType} /{maxRun}";
        }

        return card;
    }

    private int AllocateSerial(int maxRun)
    {
        if (maxRun <= 0) return 0;
        for (int attempts = 0; attempts < 100; attempts++)
        {
            int candidate = Random.Range(1, maxRun + 1);
            string key    = $"{maxRun}_{candidate}";
            if (_usedSerials.Add(key)) return candidate;
        }
        return Random.Range(1, maxRun + 1);  // fallback if pool is exhausted
    }

    // ================================================================
    // 2. DYNAMIC MATERIAL SETTER
    // ================================================================

    /// <summary>
    /// Applies all visual data from a GeneratedSportsCard to the in-world
    /// 3D card prefab assigned to this component.
    /// </summary>
    public void ApplyCardToMesh(GeneratedSportsCard card)
    {
        var profile = card.visualProfile;

        // ---- TextMeshPro text fields ----
        if (cardNameTMP)
            cardNameTMP.text = card.athleteName;

        if (subsetLabelTMP)
            subsetLabelTMP.text = card.subsetLabel;

        if (serialStampTMP)
        {
            bool numbered = profile.IsNumbered;
            serialStampTMP.gameObject.SetActive(numbered);
            if (numbered)
                serialStampTMP.text = profile.IsOneOfOne
                    ? "01/01"
                    : $"{profile.serialCurrentIndex:D2}/{profile.serialMaxPrintRun}";
        }

        if (inkColorTMP)
        {
            bool showInk = profile.hitType == HobbyDataTiers.ChaseCategory.OnCardInk ||
                           profile.hitType == HobbyDataTiers.ChaseCategory.StickerAuto ||
                           profile.hitType == HobbyDataTiers.ChaseCategory.DualInkBooklet;
            inkColorTMP.gameObject.SetActive(showInk);
            if (showInk) inkColorTMP.text = profile.autoInkColor;
        }

        // ---- Autograph overlay ----
        bool hasAuto = profile.hitType == HobbyDataTiers.ChaseCategory.OnCardInk  ||
                       profile.hitType == HobbyDataTiers.ChaseCategory.StickerAuto ||
                       profile.hitType == HobbyDataTiers.ChaseCategory.DualInkBooklet;
        if (autographOverlayObject)
        {
            autographOverlayObject.SetActive(hasAuto);
            if (hasAuto && autographRenderer)
            {
                autographRenderer.material.color = profile.autoInkColor.Contains("Gold")
                    ? Color.yellow
                    : new Color(0.2f, 0.4f, 1f);
            }
        }

        // ---- Base face foil tint + emission ----
        if (cardFaceRenderer)
        {
            var block = new MaterialPropertyBlock();
            cardFaceRenderer.GetPropertyBlock(block);

            // Foil tint
            block.SetColor("_ParallelFoilTint", profile.primaryFoilTint);

            // Holographic scroll speed (shader property)
            block.SetFloat("_HolographicIntensity", profile.holographicIntensity);

            // Emission for rare cards
            float glowStrength = 0f;
            if (profile.IsOneOfOne)
                glowStrength = oneOfOneGlowBoost;
            else if (profile.serialMaxPrintRun > 0 && profile.serialMaxPrintRun <= 5)
                glowStrength = superShortGlowBoost;
            else if (profile.hitType != HobbyDataTiers.ChaseCategory.None)
                glowStrength = baseGlowIntensity;

            block.SetColor("_EmissionColor", profile.primaryFoilTint * glowStrength);
            cardFaceRenderer.SetPropertyBlock(block);
        }
    }

    // ----------------------------------------------------------------

    private static T PickRandom<T>(List<T> list) =>
        list != null && list.Count > 0 ? list[Random.Range(0, list.Count)] : default;
}
