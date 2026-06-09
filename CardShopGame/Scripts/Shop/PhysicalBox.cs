using UnityEngine;

/// <summary>
/// Rigidbody delivery box prop.
/// Can be sealed, opened (spawns card props), emptied, then thrown in dumpster.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PhysicalBox : MonoBehaviour
{
    public enum BoxState { Sealed, Opened, Empty }
    public BoxState State { get; private set; } = BoxState.Sealed;

    public CardPackSO packTemplate;
    public int        packsInside = 6;

    private Rigidbody _rb;
    private Collider  _col;

    // ----------------------------------------------------------------

    private void Start()
    {
        _rb  = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
    }

    public void OnPickUp()
    {
        _rb.isKinematic  = true;
        _col.enabled     = false;
    }

    public void OnDrop()
    {
        _rb.isKinematic  = false;
        _col.enabled     = true;
    }

    public void OnThrow(Vector3 dir, float force)
    {
        OnDrop();
        _rb.AddForce(dir * force, ForceMode.Impulse);
        _rb.AddTorque(Random.insideUnitSphere * force * 0.4f, ForceMode.Impulse);
    }

    // Called by CarryingHand or EmployeeAI
    public void Open()
    {
        if (State != BoxState.Sealed) return;
        State = BoxState.Opened;

        if (packTemplate != null)
        {
            var cards = new System.Collections.Generic.List<CardInstance>();
            for (int i = 0; i < packsInside; i++)
                cards.AddRange(packTemplate.OpenPack());
            InventoryManager.Instance.AddRangeToBackRoom(cards);
            NotificationSystem.Show($"Opened delivery box — {cards.Count} cards to back room!");
        }

        State = BoxState.Empty;
        AudioManager.Play("box_open");
    }

    // Dumpster trigger
    private void OnCollisionEnter(Collision col)
    {
        if (State == BoxState.Empty && col.gameObject.CompareTag("Dumpster"))
        {
            ShopManager.Instance.AddFunds(0.50f);   // recycling rebate
            AudioManager.Play("trash_thud");
            Destroy(gameObject, 0.1f);
        }
    }
}
