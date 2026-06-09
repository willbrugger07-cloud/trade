using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum EmployeeRole { Cashier, Stocker }

/// <summary>
/// Hired employee NPC. Cashiers process the register queue automatically;
/// Stockers find DeliveryBox props on the floor and route cards to shelves.
/// Hourly wage deducted from shop funds by HiringManager each game-hour.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EmployeeAI : MonoBehaviour
{
    [Header("Role")]
    public EmployeeRole role;
    public float        hourlyWage = 15f;

    [Header("Waypoints")]
    public Transform employeeIdlePoint;

    private NavMeshAgent _agent;

    // ----------------------------------------------------------------

    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        StartCoroutine(WorkLoop());
        HiringManager.Instance?.RegisterEmployee(this);
    }

    private void OnDestroy() =>
        HiringManager.Instance?.UnregisterEmployee(this);

    // ----------------------------------------------------------------

    private IEnumerator WorkLoop()
    {
        while (true)
        {
            bool didWork = role == EmployeeRole.Cashier
                ? yield return StartCoroutine(CashierTask())
                : yield return StartCoroutine(StockerTask());

            if (!didWork)
                yield return new WaitForSeconds(2f);   // idle cooldown
        }
    }

    // Returns true if work was found and performed
    private IEnumerator CashierTask()
    {
        var register = FindObjectOfType<CheckoutRegister>();
        if (register == null || register.QueueLength == 0) { yield return false; yield break; }

        yield return MoveTo(register.transform.position);

        while (register.QueueLength > 0)
        {
            yield return new WaitForSeconds(2.5f);
            register.Interact(null);   // null = employee-driven, not player
        }

        yield return true;
    }

    private IEnumerator StockerTask()
    {
        var box = GameObject.FindWithTag("DeliveryBox");
        if (box == null) { yield return false; yield break; }

        yield return MoveTo(box.transform.position);

        var delivery = box.GetComponent<DeliveryBox>();
        delivery?.Interact(null);    // employee opens/stocks the box

        yield return new WaitForSeconds(4f);
        yield return true;
    }

    private IEnumerator MoveTo(Vector3 pos)
    {
        _agent.SetDestination(pos);
        yield return new WaitUntil(() =>
            !_agent.pathPending && _agent.remainingDistance < 0.6f);
    }
}
