using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Card game tournament / trade-night table.
/// Player activates the table; AI NPCs sit down, pay entry fees, and browse
/// the store afterward as potential buyers.
/// </summary>
public class TournamentTable : MonoBehaviour, IInteractable
{
    [Header("Event Settings")]
    public float entryFeePerPlayer = 10f;
    public int   tableCapacity     = 4;
    public float eventDurationSeconds = 60f;   // 1 in-game hour

    [Header("Seat Points")]
    public List<Transform> chairPositions;

    [Header("NPC Prefab")]
    public GameObject playerNPCPrefab;

    public bool IsActive { get; private set; }

    private readonly List<GameObject> _seated = new();

    // ----------------------------------------------------------------

    public void Interact(PlayerInteraction player)
    {
        if (IsActive) { NotificationSystem.Show("Tournament already in progress."); return; }
        StartCoroutine(RunEvent());
    }

    private IEnumerator RunEvent()
    {
        IsActive = true;
        int seats = Mathf.Min(tableCapacity, chairPositions.Count);
        float revenue = entryFeePerPlayer * seats;
        ShopManager.Instance.AddFunds(revenue);
        AudioManager.Play("tournament_start");
        NotificationSystem.Show($"🏆 Tournament started! Collected ${revenue:F2} in entry fees.");

        for (int i = 0; i < seats; i++)
        {
            if (playerNPCPrefab)
            {
                var npc = Instantiate(playerNPCPrefab, chairPositions[i].position,
                                      chairPositions[i].rotation);
                _seated.Add(npc);
            }
        }

        yield return new WaitForSeconds(eventDurationSeconds);

        // Release NPCs to browse the shop
        foreach (var npc in _seated)
        {
            var ai = npc.GetComponent<CustomerAI>();
            if (ai) ai.exitPoint = ShopManager.Instance.exitPoint;
            else Destroy(npc);
        }
        _seated.Clear();
        IsActive = false;
        NotificationSystem.Show("Tournament concluded — players browsing the shop.");
    }
}
