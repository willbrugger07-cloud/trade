using TMPro;
using UnityEngine;

/// <summary>
/// Updates the in-world 3D card object when a new CardInstance is assigned.
/// Controls serial stamp, autograph layer, jersey patch, and parallel foil tint.
/// </summary>
public class CardMeshController : MonoBehaviour
{
    [Header("Renderers")]
    public Renderer    cardBaseRenderer;
    public GameObject  autographLayer;
    public Renderer    jerseyPatchRenderer;

    [Header("Text Fields")]
    public TextMeshPro cardNameText;
    public TextMeshPro serialStampText;
    public TextMeshPro setBrandText;

    public CardInstance CurrentCard { get; private set; }

    // ----------------------------------------------------------------

    public void UpdateCard(CardInstance card)
    {
        CurrentCard = card;

        if (cardNameText)  cardNameText.text  = card.data.cardName;
        if (setBrandText)  setBrandText.text  = $"{card.data.seriesYear} {card.data.sport}";

        // Serial stamp
        bool numbered = !string.IsNullOrEmpty(card.serialNumber) && card.serialNumber != "0";
        if (serialStampText)
        {
            serialStampText.gameObject.SetActive(numbered);
            if (numbered) serialStampText.text = $"#{card.serialNumber}";
        }

        // Parallel foil tint via MaterialPropertyBlock
        if (cardBaseRenderer && card.layout != null)
        {
            var block = new MaterialPropertyBlock();
            cardBaseRenderer.GetPropertyBlock(block);
            block.SetColor("_ParallelFoilTint", card.layout.cardBorderGlow);
            cardBaseRenderer.SetPropertyBlock(block);
        }

        // Autograph layer
        if (autographLayer)
        {
            bool showAuto = card.isAutographed && card.layout != null &&
                            card.layout.inkStyle != CardVisualLayout.AutoType.None;
            autographLayer.SetActive(showAuto);
            if (showAuto)
            {
                var r   = autographLayer.GetComponent<Renderer>();
                if (r) r.material.color =
                    card.layout.inkStyle == CardVisualLayout.AutoType.OnCardInk &&
                    card.serialNumber == "1"
                        ? Color.yellow
                        : new Color(0.2f, 0.4f, 1f);  // blue ink default
            }
        }

        // Jersey / patch layer
        if (jerseyPatchRenderer)
        {
            bool showPatch = card.layout != null &&
                             card.layout.memorabiliaStyle != CardVisualLayout.PatchType.None;
            jerseyPatchRenderer.gameObject.SetActive(showPatch);
        }
    }
}
