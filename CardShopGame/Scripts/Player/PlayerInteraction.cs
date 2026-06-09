using UnityEngine;

/// <summary>
/// Attaches to the Main Camera. Casts a ray forward on E press
/// and calls Interact() on any IInteractable in range.
/// Also shows a crosshair tooltip with the object's name.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    public float interactRange = 3f;
    public LayerMask interactableLayer;

    [Header("UI")]
    public TMPro.TMP_Text promptLabel;  // "Press E to [interact name]"

    private IInteractable _focused;

    // ----------------------------------------------------------------

    private void Update()
    {
        if (PlayerController.UIOpen) { ClearPrompt(); return; }

        Scan();

        if (_focused != null && Input.GetKeyDown(KeyCode.E))
            _focused.Interact(GetComponent<PlayerInteraction>());
    }

    private void Scan()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out var hit, interactRange, interactableLayer))
        {
            var interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                _focused = interactable;
                if (promptLabel)
                    promptLabel.text = $"[E]  {hit.collider.gameObject.name}";
                return;
            }
        }

        ClearPrompt();
    }

    private void ClearPrompt()
    {
        _focused = null;
        if (promptLabel) promptLabel.text = string.Empty;
    }
}

/// <summary>Implement on any object the player can interact with.</summary>
public interface IInteractable
{
    void Interact(PlayerInteraction player);
}
