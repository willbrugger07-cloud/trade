using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum CustomerType { Regular, BargainHunter, Whale, Ripper }

/// <summary>
/// Full customer AI brain supporting three archetypes:
///   Regular      — buys cards from standard shelves at or near market value
///   BargainHunter — only buys Base cards priced below market value
///   Whale        — heads straight to display cases, willing to spend up to $1,000
///   Ripper       — buys a sealed pack and opens it on the spot at the counter
///
/// Life cycle:  Spawn → Browse → Register queue → Checkout → Exit
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class CustomerAI : MonoBehaviour
{
    [Header("Archetype")]
    public CustomerType type = CustomerType.Regular;

    [Header("Budget")]
    public float budget = 100f;

    public IReadOnlyList<CardInstance> Cart => _cart;
    private readonly List<CardInstance> _cart = new();

    private NavMeshAgent _agent;
    private enum State { Entering, Browsing, InQueue, WaitingPayment, Leaving }
    private State _state;

    // Set by CustomerSpawner
    [HideInInspector] public Transform exitPoint;

    // ----------------------------------------------------------------

    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        StartCoroutine(LifeCycle());
    }

    private IEnumerator LifeCycle()
    {
        _state = State.Entering;

        yield return type switch
        {
            CustomerType.BargainHunter => StartCoroutine(BargainHunterBrowse()),
            CustomerType.Whale         => StartCoroutine(WhaleBrowse()),
            CustomerType.Ripper        => StartCoroutine(RipperBrowse()),
            _                          => StartCoroutine(RegularBrowse()),
        };

        // Head to register if cart has items
        if (_cart.Count > 0)
        {
            _state = State.InQueue;
            var register = FindObjectOfType<CheckoutRegister>();
            if (register != null)
            {
                yield return MoveTo(register.transform.position);
                register.JoinQueue(this);
                _state = State.WaitingPayment;
                yield return new WaitUntil(() => _state == State.Leaving);
            }
        }

        // Exit
        _state = State.Leaving;
        if (exitPoint != null) yield return MoveTo(exitPoint.position);
        Destroy(gameObject);
    }

    // ---------------------------------------------------------------- archetypes

    private IEnumerator RegularBrowse()
    {
        var shelves = FindObjectsOfType<CardShelf>();
        foreach (var shelf in Shuffle(shelves))
        {
            if (!shelf.HasStock) continue;
            yield return MoveTo(shelf.customerInteractionPoint.position);
            yield return new WaitForSeconds(Random.Range(1.5f, 3.5f));

            var card = shelf.TakeCard();
            if (card != null && card.GetSalePrice() <= budget)
            {
                _cart.Add(card);
                budget -= card.GetSalePrice();
            }
            else if (card != null)
            {
                shelf.StockCard(card);   // put back — too expensive
            }
            if (budget <= 0) break;
        }
    }

    private IEnumerator BargainHunterBrowse()
    {
        var shelves = FindObjectsOfType<CardShelf>();
        foreach (var shelf in Shuffle(shelves))
        {
            if (!shelf.HasStock) continue;
            yield return MoveTo(shelf.customerInteractionPoint.position);
            yield return new WaitForSeconds(Random.Range(1f, 2f));

            // Only buys if priced below current market value
            var card = shelf.TakeCard();
            if (card != null)
            {
                bool isGoodDeal = card.GetSalePrice() < card.data.currentMarketValue;
                bool canAfford  = card.GetSalePrice() <= budget;
                if (isGoodDeal && canAfford)
                {
                    _cart.Add(card);
                    budget -= card.GetSalePrice();
                }
                else
                {
                    shelf.StockCard(card);
                }
            }
        }
    }

    private IEnumerator WhaleBrowse()
    {
        budget = Random.Range(300f, 1000f);
        var cases = FindObjectsOfType<PremiumDisplayCase>();

        foreach (var dc in Shuffle(cases))
        {
            if (!dc.IsOccupied) continue;
            yield return MoveTo(dc.transform.position);
            yield return new WaitForSeconds(Random.Range(3f, 8f));  // deliberate inspection

            if (dc.TryPurchase(budget, out var card))
            {
                ShopManager.Instance.AddFunds(dc.RetailPrice);
                NotificationSystem.Show($"🐳 Whale bought {card.GetDisplayName()} for ${dc.RetailPrice:F2}!");
                AudioManager.Play("big_sale");
                budget -= dc.RetailPrice;
            }
        }
    }

    private IEnumerator RipperBrowse()
    {
        // Walk to the pack-opening counter and buy / open a pack live
        var opener = FindObjectOfType<PackOpenStation>();
        if (opener == null) yield break;

        yield return MoveTo(opener.transform.position);
        yield return new WaitForSeconds(1f);

        var pulledCards = opener.CustomerRipPack(budget);
        foreach (var c in pulledCards)
        {
            NotificationSystem.Show($"Customer pulled: {c.GetDisplayName()}!");
        }
        budget = 0f;   // spent it all on the pack
    }

    // ---------------------------------------------------------------- helpers

    /// <summary>Called by CheckoutRegister after the player processes payment.</summary>
    public void ConfirmPaymentAndLeave() => _state = State.Leaving;

    private IEnumerator MoveTo(Vector3 pos)
    {
        _agent.SetDestination(pos);
        yield return new WaitUntil(() =>
            !_agent.pathPending && _agent.remainingDistance < 0.6f);
    }

    private static T[] Shuffle<T>(T[] arr)
    {
        T[] copy = (T[])arr.Clone();
        for (int i = copy.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (copy[i], copy[j]) = (copy[j], copy[i]);
        }
        return copy;
    }
}
