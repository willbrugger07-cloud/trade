using System.Collections;
using UnityEngine;

/// <summary>
/// Simulates AI customers walking in to sell their cards.
/// Spawn via CustomerSpawner — trade-in NPCs approach the register
/// and wait for the player to accept or decline the offer.
/// </summary>
public class TradeInSystem : MonoBehaviour
{
    public static TradeInSystem Instance { get; private set; }

    [Header("Offer Settings")]
    [Range(0.5f, 0.9f)]
    public float offerPercentage = 0.70f;   // 70% of market value

    [Header("Trade-In NPC Prefab")]
    public GameObject tradeInCustomerPrefab;
    public Transform  spawnPoint;

    private float _cooldown = 0f;
    [SerializeField] private float spawnIntervalSeconds = 180f;   // every 3 real minutes

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (!DayCycleManager.Instance.ShopIsOpen) return;
        _cooldown -= Time.deltaTime;
        if (_cooldown <= 0f)
        {
            _cooldown = spawnIntervalSeconds;
            TrySpawnTradeIn();
        }
    }

    private void TrySpawnTradeIn()
    {
        if (tradeInCustomerPrefab == null || spawnPoint == null) return;
        Instantiate(tradeInCustomerPrefab, spawnPoint.position, Quaternion.identity);
    }

    /// <summary>
    /// Called by TradeInCustomerAI when player accepts the offer.
    /// Card goes into back room; player's funds decrease.
    /// </summary>
    public void AcceptOffer(CardInstance card)
    {
        float price = Mathf.Round(card.GetSalePrice() * offerPercentage * 100f) / 100f;
        if (!ShopManager.Instance.SpendFunds(price)) return;

        InventoryManager.Instance.AddToBackRoom(card);
        NotificationSystem.Show($"Bought {card.GetDisplayName()} for ${price:F2}");
        AudioManager.Play("trade_in_accept");
    }

    public void DeclineOffer(CardInstance card)
    {
        NotificationSystem.Show("Offer declined.");
        AudioManager.Play("trade_in_decline");
    }
}
