using UnityEngine;

/// <summary>
/// Spawned after an online sale completes.
/// Player interacts to seal and drop it in the outbox bin — fulfills the order.
/// </summary>
public class ShippingBox : MonoBehaviour, IInteractable
{
    public CardInstance Contents { get; private set; }
    private bool _sealed;

    public void Setup(CardInstance card) => Contents = card;

    public void Interact(PlayerInteraction player)
    {
        if (_sealed) { NotificationSystem.Show("Box already sealed."); return; }
        _sealed = true;
        AudioManager.Play("box_seal");
        NotificationSystem.Show($"📫 Shipping {Contents?.GetDisplayName()} — drop in outbox!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_sealed) return;
        if (other.CompareTag("Outbox"))
        {
            NotificationSystem.Show("Order shipped! ✈️");
            AudioManager.Play("mail_sent");
            Destroy(gameObject);
        }
    }
}
