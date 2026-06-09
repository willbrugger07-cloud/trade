using UnityEngine;

/// <summary>
/// Wholesale master case containing 6 sealed booster boxes.
/// Player presses E to extract one box at a time into their carry hand.
/// </summary>
public class BoosterCaseCargo : MonoBehaviour, IInteractable
{
    [Header("Contents")]
    public string boosterSeriesId;
    public int    boxesRemaining = 6;
    public GameObject boosterBoxPrefab;

    [Header("Visual States")]
    public GameObject fullCaseMesh;
    public GameObject emptyCaseMesh;

    // ----------------------------------------------------------------

    public void Interact(PlayerInteraction player)
    {
        ExtractBox(player.CarryPivot);
    }

    private void ExtractBox(Transform handPivot)
    {
        if (boxesRemaining <= 0)
        {
            NotificationSystem.Show("Case is empty — toss it in the dumpster.");
            return;
        }

        if (!boosterBoxPrefab) return;

        var boxObj = Instantiate(boosterBoxPrefab, handPivot.position, handPivot.rotation);
        var box    = boxObj.GetComponent<PhysicalBox>();
        if (box)
        {
            box.productId = boosterSeriesId;
            box.State     = PhysicalBox.BoxState.Sealed;
        }

        boxesRemaining--;

        if (boxesRemaining == 0)
        {
            if (fullCaseMesh)  fullCaseMesh.SetActive(false);
            if (emptyCaseMesh) emptyCaseMesh.SetActive(true);
        }

        NotificationSystem.Show($"Box extracted. {boxesRemaining} remaining in case.");
        AudioManager.Play("card_place_on_mat");
    }
}
