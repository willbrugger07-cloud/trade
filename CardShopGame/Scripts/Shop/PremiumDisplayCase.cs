using UnityEngine;

/// <summary>
/// Locked glass display case for high-value / graded cards.
/// Supports a custom retail price and a markup cap that AI buyers enforce.
/// Customers linger longer and only Whale archetype will pay premium prices.
/// </summary>
public class PremiumDisplayCase : MonoBehaviour, IInteractable
{
    [Header("Pricing")]
    [Tooltip("Max multiple of graded market value a customer will pay")]
    public float markupCeiling = 2.5f;

    [Header("Visuals")]
    public MeshRenderer cardMeshRenderer;  // The 3D card prop inside the case
    public Material     emptyMaterial;
    public GameObject   glowEffect;

    public CardInstance FeaturedCard   { get; private set; }
    public float        RetailPrice    { get; private set; }
    public bool         IsOccupied     => FeaturedCard != null;

    // ----------------------------------------------------------------

    /// <summary>Player assigns a card and sets a custom price.</summary>
    public void AssignCard(CardInstance card, float price)
    {
        FeaturedCard = card;
        RetailPrice  = price;
        InventoryManager.Instance.RemoveFromBackRoom(card);
        RefreshVisuals();
        AudioManager.Play("case_place");
        NotificationSystem.Show($"{card.GetDisplayName()} placed in display case at ${price:F2}");
    }

    /// <summary>
    /// Called by Whale CustomerAI.
    /// Returns true if the price is within budget and within the fair markup ceiling.
    /// </summary>
    public bool TryPurchase(float customerBudget, out CardInstance soldCard)
    {
        soldCard = null;
        if (!IsOccupied) return false;

        float fair = FeaturedCard.GetSalePrice();
        if (RetailPrice > fair * markupCeiling)   return false;  // too greedy
        if (RetailPrice > customerBudget)          return false;  // can't afford

        soldCard     = FeaturedCard;
        FeaturedCard = null;
        RetailPrice  = 0f;
        RefreshVisuals();
        return true;
    }

    // IInteractable — player presses E to manage the case
    public void Interact(PlayerInteraction player)
    {
        if (IsOccupied)
            DisplayCaseUI.Instance?.OpenForCase(this);
        else
            DisplayCaseUI.Instance?.OpenForStocking(this);
    }

    private void RefreshVisuals()
    {
        if (glowEffect) glowEffect.SetActive(IsOccupied && FeaturedCard?.isGraded == true);
        if (cardMeshRenderer)
            cardMeshRenderer.material = IsOccupied ? null : emptyMaterial;
    }
}
