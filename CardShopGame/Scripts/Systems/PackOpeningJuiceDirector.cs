using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual pack-opening sequence controller.
/// Reveals cards one by one on click; fires shader emission effects for hits;
/// shows sport-specific subset labels and a big-pull overlay for 1-of-1s.
/// </summary>
public class PackOpeningJuiceDirector : MonoBehaviour
{
    public static PackOpeningJuiceDirector Instance { get; private set; }

    // ----------------------------------------------------------------
    // Inspector references

    [Header("Card Reveal Panel")]
    public GameObject     revealPanel;
    public Image          cardPortraitImage;
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI subsetLabelText;
    public TextMeshProUGUI serialStampText;
    public TextMeshProUGUI marketValueText;
    public Button         nextCardButton;
    public Button         closeButton;

    [Header("Big Pull Overlay")]
    public GameObject     bigPullOverlay;
    public TextMeshProUGUI bigPullNameText;
    public TextMeshProUGUI bigPullValueText;
    public ParticleSystem fireworks;

    [Header("Card Mesh (in-world)")]
    public CardMeshController meshController;

    [Header("Emission Flash")]
    public Renderer cardRenderer;
    public float    flashDuration = 0.45f;
    public Color    hitFlashColor = Color.yellow;

    // ----------------------------------------------------------------

    private List<CardInstance> _queue  = new();
    private int                _index;
    private Coroutine          _flash;

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (nextCardButton) nextCardButton.onClick.AddListener(AdvanceReveal);
        if (closeButton)    closeButton.onClick.AddListener(CloseReveal);
    }

    // ----------------------------------------------------------------
    // Public entry point called by pack-open stations

    public void BeginRevealSequence(List<CardInstance> cards)
    {
        _queue  = new List<CardInstance>(cards);
        _index  = 0;

        if (revealPanel)    revealPanel.SetActive(true);
        if (bigPullOverlay) bigPullOverlay.SetActive(false);

        RevealCard(_index);
    }

    // ----------------------------------------------------------------

    private void AdvanceReveal()
    {
        _index++;
        if (_index < _queue.Count)
        {
            RevealCard(_index);
        }
        else
        {
            CloseReveal();
        }
    }

    private void CloseReveal()
    {
        if (revealPanel)    revealPanel.SetActive(false);
        if (bigPullOverlay) bigPullOverlay.SetActive(false);
        _queue.Clear();
    }

    // ----------------------------------------------------------------

    private void RevealCard(int idx)
    {
        var card = _queue[idx];

        // ---- Text fields ----
        if (cardNameText)  cardNameText.text  = card.data.cardName;
        if (cardPortraitImage && card.data.portrait) cardPortraitImage.sprite = card.data.portrait;

        // Sport-specific subset label
        string subset = card.isAutographed
            ? $"{HobbyTerminology.GetRookieLabel(card.data.sport)} AUTO"
            : card.data.tier >= CardTier.Autograph
                ? HobbyTerminology.GetCaseHitName(card.data.sport)
                : HobbyTerminology.GetRookieLabel(card.data.sport);

        if (subsetLabelText) subsetLabelText.text = subset;

        // Serial stamp
        if (serialStampText)
        {
            if (!string.IsNullOrEmpty(card.serialNumber) && card.serialNumber != "0")
            {
                serialStampText.gameObject.SetActive(true);
                serialStampText.text = $"#{card.serialNumber}";
            }
            else
            {
                serialStampText.gameObject.SetActive(false);
            }
        }

        // Market value
        float val = SportsSeasonManager.Instance != null
            ? SportsSeasonManager.Instance.GetSeasonalValue(card)
            : card.GetSalePrice();
        if (marketValueText) marketValueText.text = $"Est. Value: ${val:F2}";

        // ---- 3D mesh ----
        meshController?.UpdateCard(card);

        // ---- Hit effects ----
        bool isHit = card.data.tier >= CardTier.Autograph || card.isAutographed ||
                     (!string.IsNullOrEmpty(card.serialNumber) && card.serialNumber == "1");

        if (isHit)
        {
            if (_flash != null) StopCoroutine(_flash);
            _flash = StartCoroutine(FlashEmission(isOneOfOne: card.serialNumber == "1"));
        }

        // ---- Big pull overlay ----
        bool isOneOfOne = card.serialNumber == "1";
        if (isOneOfOne || card.data.tier == CardTier.Immortal)
        {
            ShowBigPullOverlay(card, val);
        }

        // ---- Hype controller ----
        PackHypeController.Instance?.RegisterPull(card.data.tier);

        AudioManager.Play(isHit ? "big_pull" : "card_reveal");
    }

    // ----------------------------------------------------------------

    private void ShowBigPullOverlay(CardInstance card, float value)
    {
        if (!bigPullOverlay) return;
        bigPullOverlay.SetActive(true);

        if (bigPullNameText)
            bigPullNameText.text = $"{card.data.cardName.ToUpper()} – {HobbyTerminology.GetCaseHitName(card.data.sport)}";
        if (bigPullValueText)
            bigPullValueText.text = $"EST. VALUE: ${value:F2}";

        fireworks?.Play();
        AudioManager.Play("big_pull");
        StartCoroutine(AutoDismissOverlay());
    }

    private IEnumerator AutoDismissOverlay()
    {
        yield return new WaitForSeconds(4f);
        fireworks?.Stop();
        if (bigPullOverlay) bigPullOverlay.SetActive(false);
    }

    // ----------------------------------------------------------------

    private IEnumerator FlashEmission(bool isOneOfOne)
    {
        if (!cardRenderer) yield break;

        var block  = new MaterialPropertyBlock();
        Color peak = isOneOfOne ? new Color(1f, 0.84f, 0f) : hitFlashColor;  // gold for 1/1
        float t    = 0f;

        while (t < flashDuration)
        {
            float alpha = Mathf.Sin(t / flashDuration * Mathf.PI);
            cardRenderer.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", peak * alpha * (isOneOfOne ? 3f : 1.5f));
            cardRenderer.SetPropertyBlock(block);
            t += Time.deltaTime;
            yield return null;
        }

        cardRenderer.GetPropertyBlock(block);
        block.SetColor("_EmissionColor", Color.black);
        cardRenderer.SetPropertyBlock(block);
    }
}
