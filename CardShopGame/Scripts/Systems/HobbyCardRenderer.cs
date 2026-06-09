using TMPro;
using UnityEngine;
using SportsCardTycoon.Core;

/// <summary>
/// Two critical production functions:
///
/// 1. UpdateCardMesh(LiveCardInstance) — updates all TextMeshPro fields and
///    toggles renderer states for patch frames, signature overlays, and
///    serial-stamp text on the back mesh.
///
/// 2. ApplyMaterialProperties(LiveCardInstance) — pushes shader parameters
///    (metallic, roughness, iridescent refraction, emission) onto the card
///    face renderer via MaterialPropertyBlock so no material assets are
///    permanently modified at runtime.
/// </summary>
public class HobbyCardRenderer : MonoBehaviour
{
    public static HobbyCardRenderer Instance { get; private set; }

    // ----------------------------------------------------------------
    // Inspector references

    [Header("Front Face")]
    public Renderer    cardFaceRenderer;

    [Header("Text Fields (Front)")]
    public TextMeshPro athleteNameTMP;
    public TextMeshPro teamCityTMP;
    public TextMeshPro setLabelTMP;

    [Header("Text Fields (Back)")]
    public TextMeshPro serialStampTMP;

    [Header("Overlay Objects")]
    public GameObject  signatureOverlay;
    public Renderer    signatureRenderer;
    public GameObject  patchEmbedObject;
    public Renderer    patchRenderer;
    public GameObject  goldenFrameOverlay;    // Enabled for 1-of-1 cards

    // ----------------------------------------------------------------
    // Shader property IDs (cached to avoid string lookups per frame)

    private static readonly int PropMetallic        = Shader.PropertyToID("_Metallic");
    private static readonly int PropSmoothness      = Shader.PropertyToID("_Glossiness");
    private static readonly int PropParallelTint     = Shader.PropertyToID("_ParallelFoilTint");
    private static readonly int PropIridescence      = Shader.PropertyToID("_IridescentIntensity");
    private static readonly int PropHoloRefraction   = Shader.PropertyToID("_HolographicRefraction");
    private static readonly int PropEmission         = Shader.PropertyToID("_EmissionColor");

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ================================================================
    // 1. DYNAMIC 3D MODEL CONSTRUCTOR
    // ================================================================

    public void UpdateCardMesh(LiveCardInstance card)
    {
        var attr = card.visualAttributes;

        // ---- Front text fields ----
        if (athleteNameTMP) athleteNameTMP.text = card.athleteName;
        if (teamCityTMP)    teamCityTMP.text    = card.teamDesignation;
        if (setLabelTMP)    setLabelTMP.text    = card.cardSetLabel;

        // ---- Back serial stamp ----
        if (serialStampTMP)
        {
            if (attr.IsNumbered)
            {
                serialStampTMP.gameObject.SetActive(true);
                serialStampTMP.text = attr.IsOneOfOne
                    ? "01/01"
                    : $"{attr.currentSerialIndex:D2}/{attr.maxPrintRun}";

                serialStampTMP.color = attr.IsOneOfOne
                    ? new Color(1f, 0.84f, 0f)   // gold
                    : Color.white;
            }
            else
            {
                serialStampTMP.gameObject.SetActive(false);
            }
        }

        // ---- Signature overlay ----
        bool hasSignature = attr.cardHitType == HobbyEnums.HitCategory.OnCardInk  ||
                            attr.cardHitType == HobbyEnums.HitCategory.StickerSignature ||
                            attr.cardHitType == HobbyEnums.HitCategory.DualInkBooklet;

        if (signatureOverlay) signatureOverlay.SetActive(hasSignature);
        if (hasSignature && signatureRenderer)
        {
            bool goldInk = attr.inkColorHex == "#FFD700";
            signatureRenderer.material.color = goldInk
                ? new Color(1f, 0.84f, 0f)
                : new Color(0.18f, 0.38f, 0.93f);  // blue ink default
        }

        // ---- Patch embed ----
        bool hasPatch = attr.cardHitType == HobbyEnums.HitCategory.PlayerWornPatch ||
                        attr.cardHitType == HobbyEnums.HitCategory.PrimeMultiColor  ||
                        attr.cardHitType == HobbyEnums.HitCategory.SlabbedLogoman;

        if (patchEmbedObject) patchEmbedObject.SetActive(hasPatch);
        if (hasPatch && patchRenderer)
        {
            bool isPrime = attr.cardHitType == HobbyEnums.HitCategory.PrimeMultiColor;
            bool isLogoman = attr.cardHitType == HobbyEnums.HitCategory.SlabbedLogoman;
            patchRenderer.material.color = isLogoman
                ? new Color(1f, 0.84f, 0f)
                : isPrime ? new Color(0.8f, 0.2f, 0.2f) : Color.white;
        }

        // ---- 1-of-1 golden frame overlay ----
        if (goldenFrameOverlay) goldenFrameOverlay.SetActive(attr.IsOneOfOne);

        // ---- Material shader ----
        ApplyMaterialProperties(card);
    }

    // ================================================================
    // 2. PROCEDURAL MATERIAL PROPERTIES SETTER
    // ================================================================

    public void ApplyMaterialProperties(LiveCardInstance card)
    {
        if (!cardFaceRenderer) return;

        var attr  = card.visualAttributes;
        var block = new MaterialPropertyBlock();
        cardFaceRenderer.GetPropertyBlock(block);

        // ---- Parallel foil tint ----
        block.SetColor(PropParallelTint, attr.parallelColorTint);

        // ---- Stock-based metallic + smoothness ----
        float metallic, smoothness, iridescence, holoRefraction;

        switch (attr.stockMaterial)
        {
            case HobbyEnums.CardStockType.VintagePaper:
                metallic = 0.00f; smoothness = 0.20f; iridescence = 0.00f; holoRefraction = 0.00f;
                break;
            case HobbyEnums.CardStockType.ChromiumFoil:
                metallic = 0.80f; smoothness = 0.85f; iridescence = 0.20f; holoRefraction = 0.10f;
                break;
            case HobbyEnums.CardStockType.OpticLaser:
                metallic = 0.50f; smoothness = 0.95f; iridescence = 0.70f; holoRefraction = 0.60f;
                break;
            case HobbyEnums.CardStockType.AcetateClear:
                metallic = 0.10f; smoothness = 1.00f; iridescence = 0.90f; holoRefraction = 0.90f;
                break;
            case HobbyEnums.CardStockType.GildedTimber:
                metallic = 1.00f; smoothness = 0.70f; iridescence = 0.30f; holoRefraction = 0.20f;
                break;
            default:
                metallic = 0f; smoothness = 0.3f; iridescence = 0f; holoRefraction = 0f;
                break;
        }

        // ---- Pattern overlay boosts refraction ----
        switch (attr.patternOverlay)
        {
            case HobbyEnums.ParallelPattern.RefractorSilver:
                iridescence   = Mathf.Min(1f, iridescence + 0.20f);
                holoRefraction = Mathf.Min(1f, holoRefraction + 0.15f);
                break;
            case HobbyEnums.ParallelPattern.MojoDiamond:
                iridescence   = Mathf.Min(1f, iridescence + 0.40f);
                holoRefraction = Mathf.Min(1f, holoRefraction + 0.35f);
                break;
            case HobbyEnums.ParallelPattern.ZebraStripe:
                iridescence   = Mathf.Min(1f, iridescence + 0.30f);
                break;
            case HobbyEnums.ParallelPattern.GenesisNebula:
                iridescence    = 1.00f;
                holoRefraction = 1.00f;
                metallic       = 1.00f;
                smoothness     = 1.00f;
                break;
        }

        block.SetFloat(PropMetallic,       metallic);
        block.SetFloat(PropSmoothness,     smoothness);
        block.SetFloat(PropIridescence,    iridescence);
        block.SetFloat(PropHoloRefraction, holoRefraction);

        // ---- Emission for high-rarity cards ----
        Color emissionColor = Color.black;

        if (attr.IsOneOfOne)
        {
            emissionColor = new Color(1f, 0.84f, 0f) * 6f;  // gold burst
        }
        else if (attr.cardHitType == HobbyEnums.HitCategory.SlabbedLogoman ||
                 attr.cardHitType == HobbyEnums.HitCategory.DualInkBooklet)
        {
            emissionColor = attr.parallelColorTint * 3.5f;
        }
        else if (attr.cardHitType != HobbyEnums.HitCategory.None)
        {
            emissionColor = attr.parallelColorTint * 1.4f;
        }
        else if (attr.patternOverlay == HobbyEnums.ParallelPattern.GenesisNebula)
        {
            emissionColor = attr.parallelColorTint * 2.0f;
        }

        block.SetColor(PropEmission, emissionColor);

        cardFaceRenderer.SetPropertyBlock(block);
    }
}
