using UnityEngine;

/// <summary>
/// Physical open/closed sign on the front door.
/// Player presses E to toggle, which drives DayCycleManager state.
/// </summary>
public class OpenSignNode : MonoBehaviour, IInteractable
{
    public MeshRenderer signRenderer;
    public Material     openMaterial;
    public Material     closedMaterial;

    public bool IsOpen { get; private set; }

    // ----------------------------------------------------------------

    public void Interact(PlayerInteraction player)
    {
        if (DayCycleManager.Instance?.CurrentState == DayCycleManager.GameState.SummaryScreen)
        {
            NotificationSystem.Show("The day has ended. Prepare for tomorrow.");
            return;
        }

        IsOpen = !IsOpen;

        if (signRenderer)
            signRenderer.material = IsOpen ? openMaterial : closedMaterial;

        if (IsOpen)
        {
            DayCycleManager.Instance?.TransitionToState(DayCycleManager.GameState.ShopOpen);
            AudioManager.Play("door_open");
        }
        else
        {
            DayCycleManager.Instance?.TransitionToState(DayCycleManager.GameState.DayPreparation);
            NotificationSystem.Show("Doors locked. Existing customers will finish up.");
            AudioManager.Play("door_close");
        }
    }
}
