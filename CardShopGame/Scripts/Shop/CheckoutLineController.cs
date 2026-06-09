using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Manages a single-file queue behind the register.
/// Customers step forward as the head of the line is served.
/// Attach alongside CheckoutRegister.
/// </summary>
public class CheckoutLineController : MonoBehaviour
{
    [Header("Queue Nodes (ordered front → back)")]
    public List<Transform> queueNodes;

    private readonly List<CustomerAI> _line = new();

    public bool  IsFull => _line.Count >= queueNodes.Count;
    public int   Length => _line.Count;

    // ----------------------------------------------------------------

    public bool TryJoin(CustomerAI customer)
    {
        if (IsFull) return false;
        _line.Add(customer);
        RefreshPositions();
        return true;
    }

    /// <summary>Serve the front customer; shift everyone forward.</summary>
    public void ServeNext()
    {
        if (_line.Count == 0) return;

        var front = _line[0];
        _line.RemoveAt(0);
        front.ConfirmPaymentAndLeave();
        RefreshPositions();
    }

    private void RefreshPositions()
    {
        for (int i = 0; i < _line.Count && i < queueNodes.Count; i++)
        {
            var agent = _line[i].GetComponent<NavMeshAgent>();
            agent?.SetDestination(queueNodes[i].position);
        }
    }
}
