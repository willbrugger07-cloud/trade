using UnityEngine;

/// <summary>
/// Counterfeit detection desk. Walk-in trade cards have an 8% chance of being
/// fake. Player inspects the card under magnification for visual tells.
/// Selling a verified fake tanks shop reputation; falsely rejecting a real card
/// also has a small reputation penalty.
/// </summary>
public class AuthenticationDesk : MonoBehaviour, IInteractable
{
    [Header("Counterfeit Settings")]
    [Range(0f, 1f)] public float fakeCardChance = 0.08f;

    public CardInstance InspectedCard { get; private set; }

    // Hidden flaws — only visible once player clicks "Inspect" in the UI
    public bool BlurryFont         { get; private set; }
    public bool WrongColorBanding  { get; private set; }
    public bool IsCounterfeit      { get; private set; }
    public bool FlawsRevealed      { get; private set; }

    private float _reputationPenaltyForMistake = 5f;

    // ----------------------------------------------------------------

    public void Interact(PlayerInteraction player) =>
        AuthDeskUI.Instance?.Open(this);

    // ----------------------------------------------------------------

    public void LoadCard(CardInstance card)
    {
        InspectedCard  = card;
        FlawsRevealed  = false;
        IsCounterfeit  = Random.value < fakeCardChance;

        if (IsCounterfeit)
        {
            BlurryFont        = Random.value > 0.5f;
            WrongColorBanding = !BlurryFont;
        }
        else
        {
            BlurryFont = WrongColorBanding = false;
        }

        AuthDeskUI.Instance?.Refresh(this);
        AudioManager.Play("card_place_on_mat");
    }

    /// <summary>Player activates magnifier — reveals hidden flaws.</summary>
    public void InspectUnderMagnifier()
    {
        FlawsRevealed = true;
        AuthDeskUI.Instance?.Refresh(this);
        AudioManager.Play("magnifier_click");
    }

    // ----------------------------------------------------------------

    /// <summary>Player clicks "Authenticate" (approve).</summary>
    public void PassAuthentication()
    {
        if (IsCounterfeit)
        {
            NotificationSystem.Show("⚠️ You verified a FAKE! Selling it will hurt your reputation.");
            // Flag the card — ShopManager penalizes reputation if it later sells
            InspectedCard.data.cardName = "[FAKE] " + InspectedCard.data.cardName;
        }
        else
        {
            NotificationSystem.Show("✅ Card authenticated — genuine!");
        }
        InventoryManager.Instance.AddToBackRoom(InspectedCard);
        Clear();
    }

    /// <summary>Player clicks "Reject" (flag as fake).</summary>
    public void RejectAsFake()
    {
        if (IsCounterfeit)
        {
            NotificationSystem.Show("🎉 Counterfeit caught! The customer was sent packing.");
            AudioManager.Play("auth_caught");
        }
        else
        {
            NotificationSystem.Show("❌ False accusation — that was real. Customer left angry.");
            ShopManager.Instance.SpendFunds(_reputationPenaltyForMistake, "auth_penalty");
        }
        Clear();
    }

    private void Clear()
    {
        InspectedCard = null;
        AuthDeskUI.Instance?.Close();
    }
}
