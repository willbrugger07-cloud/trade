using UnityEngine;

/// <summary>
/// Attached to display cases. Has a daily chance of getting smudged,
/// slightly reducing sales. Player cleans it with E to restore rep.
/// </summary>
public class DisplayMaintenanceNode : MonoBehaviour, IInteractable
{
    public bool       isSmudged;
    public GameObject smudgeVisual;

    [Range(0f, 1f)] public float dirtyChancePerDay = 0.20f;

    // ----------------------------------------------------------------

    private void OnEnable()  => DayCycleManager.OnNewDay += RollDirt;
    private void OnDisable() => DayCycleManager.OnNewDay -= RollDirt;

    private void RollDirt()
    {
        if (!isSmudged && Random.value < dirtyChancePerDay)
        {
            isSmudged = true;
            if (smudgeVisual) smudgeVisual.SetActive(true);
        }
    }

    public void Interact(PlayerInteraction player)
    {
        if (!isSmudged)
        {
            NotificationSystem.Show("Display is already clean.");
            return;
        }
        isSmudged = false;
        if (smudgeVisual) smudgeVisual.SetActive(false);
        ReputationManager.Instance?.RecordPositiveEvent(0.25f);
        AudioManager.Play("sweep_clean");
        NotificationSystem.Show("Display polished.");
    }
}
