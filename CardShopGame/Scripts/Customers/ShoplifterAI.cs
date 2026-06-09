using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Shoplifter archetype — sneaks in, grabs a high-value card from a shelf
/// or display case, and sprints for the exit.
/// SecuritySystem can intercept them; the player can also tackle them (E key).
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class ShoplifterAI : MonoBehaviour, IInteractable
{
    [Header("Behaviour")]
    public float normalSpeed  = 3f;
    public float sprintSpeed  = 7f;
    public float caseHoverTime = 8f;   // time spent "browsing" before grabbing

    private NavMeshAgent _agent;
    private CardInstance _stolenCard;
    private bool _isFleeing;
    private Transform _exitPoint;

    // ----------------------------------------------------------------

    private void Start()
    {
        _agent      = GetComponent<NavMeshAgent>();
        _exitPoint  = ShopManager.Instance.exitPoint;
        _agent.speed = normalSpeed;
        StartCoroutine(ShopliftRoutine());
    }

    private IEnumerator ShopliftRoutine()
    {
        // 1. Blend in — browse for a bit
        yield return new WaitForSeconds(Random.Range(5f, 12f));

        // 2. Target the highest-value unguarded card
        var shelf = FindTargetShelf();
        if (shelf == null) { LeaveEmpty(); yield break; }

        _agent.SetDestination(shelf.customerInteractionPoint.position);
        yield return new WaitUntil(() =>
            !_agent.pathPending && _agent.remainingDistance < 0.6f);

        yield return new WaitForSeconds(Random.Range(2f, 5f));   // linger

        // 3. Grab and sprint
        _stolenCard = shelf.TakeCard();
        if (_stolenCard == null) { LeaveEmpty(); yield break; }

        _isFleeing    = true;
        _agent.speed  = sprintSpeed;
        _agent.SetDestination(_exitPoint.position);

        NotificationSystem.Show($"🚨 SHOPLIFTER! {_stolenCard.GetDisplayName()} stolen! Press E to stop them.");
        AudioManager.Play("shoplifter_alarm");

        yield return new WaitUntil(() =>
            !_agent.pathPending && _agent.remainingDistance < 0.8f);

        // Made it to the exit — card is gone
        NotificationSystem.Show($"Shoplifter escaped with {_stolenCard.GetDisplayName()}!");
        Destroy(gameObject);
    }

    // Player presses E on the shoplifter to intercept
    public void Interact(PlayerInteraction player)
    {
        if (!_isFleeing) return;
        StopAllCoroutines();
        _agent.ResetPath();

        // Return stolen card
        if (_stolenCard != null)
        {
            InventoryManager.Instance.AddToBackRoom(_stolenCard);
            NotificationSystem.Show($"Shoplifter caught! {_stolenCard.GetDisplayName()} recovered.");
            AudioManager.Play("shoplifter_caught");
        }

        Destroy(gameObject, 0.2f);
    }

    private CardShelf FindTargetShelf()
    {
        CardShelf best  = null;
        float     bestV = 0f;
        foreach (var shelf in FindObjectsOfType<CardShelf>())
        {
            if (!shelf.HasStock) continue;
            float v = shelf.Stock[shelf.Stock.Count - 1].GetSalePrice();
            if (v > bestV) { bestV = v; best = shelf; }
        }
        return best;
    }

    private void LeaveEmpty()
    {
        if (_exitPoint) _agent.SetDestination(_exitPoint.position);
        Destroy(gameObject, 5f);
    }
}
