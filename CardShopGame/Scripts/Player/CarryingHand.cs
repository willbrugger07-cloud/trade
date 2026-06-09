using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Manages what the player is physically holding.
/// Supports picking up PhysicalBox props and throwing them.
/// Also handles stacking card units before restocking a shelf.
/// </summary>
public class CarryingHand : MonoBehaviour
{
    [Header("Carry Settings")]
    public Transform holdPivot;          // Where held item floats in front of camera
    public float     throwForce = 8f;
    public float     pickupRange = 2.5f;
    public LayerMask pickupLayer;

    // Current grabbed box
    private PhysicalBox   _heldBox;
    private Rigidbody     _heldRb;

    // Card stacking for shelf-restocking
    public SportsCard StackedCardType   { get; private set; }
    public int        StackCount        { get; private set; }
    public int        MaxStack          => 20;
    public bool       HandEmpty         => _heldBox == null && StackCount == 0;

    // ----------------------------------------------------------------

    private void Update()
    {
        if (PlayerController.UIOpen) return;

        if (_heldBox != null)
        {
            _heldBox.transform.position = holdPivot.position;
            _heldBox.transform.rotation = holdPivot.rotation;

            if (Input.GetMouseButtonDown(0))   // LMB — throw
                ThrowHeld();
            if (Input.GetKeyDown(KeyCode.G))   // G — drop
                DropHeld();
        }
        else if (Input.GetKeyDown(KeyCode.F))  // F — pick up box in range
        {
            TryPickUp();
        }
    }

    // ----------------------------------------------------------------

    private void TryPickUp()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRange, pickupLayer);
        foreach (var col in hits)
        {
            var box = col.GetComponent<PhysicalBox>();
            if (box != null) { GrabBox(box); return; }
        }
    }

    private void GrabBox(PhysicalBox box)
    {
        _heldBox = box;
        _heldRb  = box.GetComponent<Rigidbody>();
        box.OnPickUp();
    }

    private void DropHeld()
    {
        _heldBox.OnDrop();
        _heldBox = null;
        _heldRb  = null;
    }

    private void ThrowHeld()
    {
        _heldBox.OnThrow(Camera.main.transform.forward, throwForce);
        _heldBox = null;
        _heldRb  = null;
    }

    // ---------------------------------------------------------------- card stacking

    public bool PickUpCards(SportsCard type, int amount, GameObject visualPrefab)
    {
        if (StackCount > 0 && StackedCardType != type)
        {
            NotificationSystem.Show("Hands are holding a different card type.");
            return false;
        }
        StackedCardType = type;
        StackCount      = Mathf.Min(StackCount + amount, MaxStack);
        return true;
    }

    public void StockShelf(CardShelf shelf)
    {
        if (StackCount == 0 || shelf == null) return;

        var instance = new CardInstance(StackedCardType);
        while (StackCount > 0 && shelf.StockCard(instance))
        {
            StackCount--;
            instance = new CardInstance(StackedCardType);
        }

        if (StackCount == 0) StackedCardType = null;
    }

    public void ClearHands()
    {
        StackedCardType = null;
        StackCount      = 0;
        if (_heldBox != null) DropHeld();
    }
}
