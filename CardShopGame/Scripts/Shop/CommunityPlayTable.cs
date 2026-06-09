using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Community play table where customers buy and open packs on the premises.
/// Generates passive revenue; leaves trash the player must clear before
/// the next customer can sit.
/// </summary>
public class CommunityPlayTable : MonoBehaviour, IInteractable
{
    [Header("Seating")]
    public List<Transform> seatNodes;

    [Header("Trash")]
    public GameObject trashPrefab;
    public bool       HasTrash { get; private set; }

    private readonly List<GameObject> _sitters = new();

    // ----------------------------------------------------------------

    public void Interact(PlayerInteraction player)
    {
        if (HasTrash)
        {
            CleanTrash();
            return;
        }
        NotificationSystem.Show("Play table is ready for customers.");
    }

    public bool TryAssignCustomer(GameObject customerPrefab, CardPackSO pack)
    {
        if (_sitters.Count >= seatNodes.Count) return false;

        float revenue = pack.packPrice;
        ShopManager.Instance.AddFunds(revenue);

        int seatIdx = _sitters.Count;
        var sitter  = Instantiate(customerPrefab, seatNodes[seatIdx].position, seatNodes[seatIdx].rotation);
        _sitters.Add(sitter);

        StartCoroutine(RipSession(sitter));
        return true;
    }

    private IEnumerator RipSession(GameObject sitter)
    {
        yield return new WaitForSeconds(Random.Range(15f, 30f));
        _sitters.Remove(sitter);
        Destroy(sitter);

        if (!HasTrash)
        {
            HasTrash = true;
            if (trashPrefab) trashPrefab.SetActive(true);
        }
    }

    public void CleanTrash()
    {
        HasTrash = false;
        if (trashPrefab) trashPrefab.SetActive(false);
        NotificationSystem.Show("Table cleaned.");
        AudioManager.Play("sweep_clean");
    }
}
