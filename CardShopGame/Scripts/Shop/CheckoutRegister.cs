using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The cash register — manages the customer queue and player-initiated checkout.
/// Place on your physical register object with a collider on the "Interactable" layer.
/// </summary>
public class CheckoutRegister : MonoBehaviour, IInteractable
{
    private readonly Queue<CustomerAI> _queue = new();
    public int QueueLength => _queue.Count;

    // ----------------------------------------------------------------

    /// <summary>Called by CustomerAI when they are ready to pay.</summary>
    public void JoinQueue(CustomerAI customer)
    {
        _queue.Enqueue(customer);
        AudioManager.Play("customer_queue");
    }

    // IInteractable — player presses E at the register
    public void Interact(PlayerInteraction player)
    {
        if (_queue.Count == 0)
        {
            NotificationSystem.Show("No customers in line.");
            return;
        }

        ProcessNextCustomer();
    }

    private void ProcessNextCustomer()
    {
        CustomerAI customer = _queue.Dequeue();

        float total = 0f;
        foreach (var card in customer.Cart)
            total += card.GetSalePrice();

        ShopManager.Instance.AddFunds(total);
        NotificationSystem.Show($"Sale complete: ${total:F2}");
        AudioManager.Play("register_cha_ching");

        customer.ConfirmPaymentAndLeave();
    }
}
