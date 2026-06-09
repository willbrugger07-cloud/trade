using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Hired stocker employee. Finds unopened delivery boxes, walks them to the
/// matching shelf, stocks until empty, then disposes of the box.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class RestockerAI : MonoBehaviour
{
    public Transform holdPivot;

    private NavMeshAgent   _agent;
    private PhysicalBox    _heldBox;

    // ----------------------------------------------------------------

    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        StartCoroutine(Brain());
    }

    private IEnumerator Brain()
    {
        while (true)
        {
            if (_heldBox == null)
            {
                var box = FindObjectOfType<PhysicalBox>();
                if (box != null && box.State == PhysicalBox.BoxState.Sealed)
                {
                    _agent.SetDestination(box.transform.position);
                    yield return new WaitUntil(() => !_agent.pathPending && _agent.remainingDistance < 0.6f);

                    _heldBox = box;
                    _heldBox.transform.SetParent(holdPivot);
                    _heldBox.transform.localPosition = Vector3.zero;
                    _heldBox.OnPickUp();
                }
            }
            else
            {
                var shelf = FindObjectOfType<CardShelf>();
                if (shelf != null && shelf.NeedsRestock)
                {
                    _agent.SetDestination(shelf.transform.position);
                    yield return new WaitUntil(() => !_agent.pathPending && _agent.remainingDistance < 0.6f);

                    yield return new WaitForSeconds(2f);
                    shelf.RestockFromBox(_heldBox);

                    if (_heldBox.IsEmpty)
                    {
                        Destroy(_heldBox.gameObject);
                        _heldBox = null;
                    }
                }
                else
                {
                    _heldBox.transform.SetParent(null);
                    _heldBox.OnDrop();
                    _heldBox = null;
                }
            }

            yield return new WaitForSeconds(1.5f);
        }
    }
}
