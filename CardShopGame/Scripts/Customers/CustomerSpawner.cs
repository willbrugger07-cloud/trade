using System.Collections;
using UnityEngine;

/// <summary>
/// Spawns regular customers, shoplifters, and trade-in customers
/// based on time of day and shop level.
/// </summary>
public class CustomerSpawner : MonoBehaviour
{
    public static CustomerSpawner Instance { get; private set; }

    [Header("Customer Prefabs")]
    public GameObject regularPrefab;
    public GameObject bargainHunterPrefab;
    public GameObject whalePrefab;
    public GameObject ripperPrefab;
    public GameObject shoplifterPrefab;

    [Header("Spawn Timing")]
    public float baseIntervalSeconds = 15f;
    public float shoplifterChance    = 0.05f;  // 5% chance per spawn event

    private Coroutine _spawnLoop;

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void StartSpawning() => _spawnLoop ??= StartCoroutine(SpawnLoop());
    public void StopSpawning()
    {
        if (_spawnLoop != null) { StopCoroutine(_spawnLoop); _spawnLoop = null; }
    }

    public void SpawnTradeCustomer()
    {
        // Called by TradeNightManager — reuses regular prefab with Trade archetype intent
        var spawn = ShopManager.Instance?.spawnPoint;
        if (spawn == null || regularPrefab == null) return;
        var go = Instantiate(regularPrefab, spawn.position, Quaternion.identity);
        var ai = go.GetComponent<CustomerAI>();
        if (ai) ai.exitPoint = ShopManager.Instance.exitPoint;
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            // Dead store: nobody shows up voluntarily below rep 20
            if (ReputationManager.Instance != null && ReputationManager.Instance.IsDeadStore())
            {
                yield return new WaitForSeconds(10f);
                continue;
            }

            // Interval scales with rep (high rep → faster spawns) and shop level
            float repMod   = ReputationManager.Instance?.SpawnIntervalModifier() ?? 1f;
            float lvlMod   = ShopUpgradeSystem.Instance ? 1f / ShopUpgradeSystem.Instance.ShopLevel : 1f;
            float interval = baseIntervalSeconds * repMod * lvlMod;
            yield return new WaitForSeconds(Mathf.Max(5f, interval));

            if (!DayCycleManager.Instance.ShopIsOpen) continue;

            SpawnCustomer();
        }
    }

    private void SpawnCustomer()
    {
        Transform spawn = ShopManager.Instance.spawnPoint;
        Transform exit  = ShopManager.Instance.exitPoint;

        if (Random.value < shoplifterChance)
        {
            if (shoplifterPrefab)
                Instantiate(shoplifterPrefab, spawn.position, Quaternion.identity);
            return;
        }

        float rep  = ReputationManager.Instance?.ReputationScore ?? 50f;

        // High rep boosts Whale probability; low rep suppresses it
        float whaleThreshold = Mathf.Lerp(0.97f, 0.85f, rep / 100f);
        float roll = Random.value;

        GameObject prefab = roll < 0.50f              ? regularPrefab
                          : roll < 0.70f              ? bargainHunterPrefab
                          : roll < whaleThreshold     ? ripperPrefab
                          :                             whalePrefab;

        if (prefab == null) return;

        var go = Instantiate(prefab, spawn.position, Quaternion.identity);
        var ai = go.GetComponent<CustomerAI>();
        if (ai) ai.exitPoint = exit;
    }
}
