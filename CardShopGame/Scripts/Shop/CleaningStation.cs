using UnityEngine;

/// <summary>
/// Card cleaning and pre-grading desk.
/// Surface smudge can be scrubbed away via minigame input.
/// Corner wear is permanent and cannot be cleaned.
/// A clean card (smudge == 0) receives a hidden grade bonus when submitted.
/// </summary>
public class CleaningStation : MonoBehaviour, IInteractable
{
    public CardInstance ActiveCard       { get; private set; }
    public float        SurfaceSmudge   { get; private set; }  // 0 = spotless
    public float        CornerWear      { get; private set; }  // permanent
    public bool         IsOccupied      => ActiveCard != null;

    [Header("Cleaning Rate")]
    [Tooltip("Smudge removed per wipe action call")]
    public float smudgePerWipe = 8f;

    // Applied by GradingManager.SubmitCard when this station cleaned the card
    public const float CleanGradeBonus = 0.5f;

    // ----------------------------------------------------------------

    public void Interact(PlayerInteraction player) =>
        CleaningUI.Instance?.Open(this);

    // ----------------------------------------------------------------

    /// <summary>Place a raw card on the mat to begin cleaning.</summary>
    public void PlaceCard(CardInstance card)
    {
        if (IsOccupied) { NotificationSystem.Show("Station is occupied."); return; }

        ActiveCard   = card;
        SurfaceSmudge = Random.Range(20f, 95f);
        CornerWear   = card.condition == CardCondition.GemMint ? 0f
                     : (float)(CardCondition.GemMint - card.condition) * 12f;

        InventoryManager.Instance.RemoveFromBackRoom(card);
        CleaningUI.Instance?.Refresh(this);
        AudioManager.Play("card_place_on_mat");
    }

    /// <summary>Called each time the player performs a wipe action in the minigame.</summary>
    public void Wipe()
    {
        if (!IsOccupied) return;
        SurfaceSmudge = Mathf.Max(0f, SurfaceSmudge - smudgePerWipe);
        CleaningUI.Instance?.Refresh(this);

        if (SurfaceSmudge == 0f)
        {
            NotificationSystem.Show("Card surface is spotless! Ready to grade.");
            AudioManager.Play("clean_complete");
        }
    }

    /// <summary>Return the card to back room (optionally with clean bonus flag).</summary>
    public void RemoveCard()
    {
        if (!IsOccupied) return;
        // Tag card as clean so GradingManager can apply the grade bonus
        if (SurfaceSmudge == 0f)
            ActiveCard.condition = (CardCondition)Mathf.Min((int)ActiveCard.condition + 1,
                                                             (int)CardCondition.GemMint);
        InventoryManager.Instance.AddToBackRoom(ActiveCard);
        ActiveCard = null;
        CleaningUI.Instance?.Close();
    }
}
