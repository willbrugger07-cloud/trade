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

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            float interval = baseIntervalSeconds / ShopUpgradeSystem.Instance.ShopLevel;
            yield return new WaitForSeconds(interval);

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

        float roll = Random.value;
        GameObject prefab = roll < 0.55f ? regularPrefab
                          : roll < 0.75f ? bargainHunterPrefab
                          : roll < 0.90f ? whalePrefab
                          :                ripperPrefab;

        if (prefab == null) return;

        var go  = Instantiate(prefab, spawn.position, Quaternion.identity);
        var ai  = go.GetComponent<CustomerAI>();
        if (ai) ai.exitPoint = exit;
    }
}
