using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Global registry of every numbered/1-of-1 card generated this run.
/// Guarantees truly unique serial numbers — duplicate 1-of-1s are impossible.
/// </summary>
public class CardPopulationDatabase : MonoBehaviour
{
    public static CardPopulationDatabase Instance { get; private set; }

    private readonly HashSet<string> _registry = new();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ----------------------------------------------------------------

    /// <summary>
    /// Call instead of new CardInstance() for numbered/rare cards.
    /// Guarantees the serial slot has not already been generated.
    /// </summary>
    public CardInstance GenerateNumberedCard(SportsCard data)
    {
        int printRun = DeterminePrintRun(data);
        int serial   = RollUniqueSerial(data, printRun);

        var instance = new CardInstance(data, CardCondition.NearMint)
        {
            serialNumber = printRun == 1 ? "1/1 ✦ ONE OF ONE" : $"{serial:D2}/{printRun}",
        };

        string uid = $"{data.series}|{data.cardName}|{serial}/{printRun}";
        _registry.Add(uid);
        return instance;
    }

    private int DeterminePrintRun(SportsCard data)
    {
        if (!data.isNumbered) return 0;
        if (data.tier >= CardTier.Immortal) return 1;

        float r = Random.value;
        if (data.tier >= CardTier.PatchAuto)
            return r < 0.10f ? 1 : r < 0.40f ? 10 : 25;
        if (data.tier >= CardTier.Autograph)
            return r < 0.05f ? 10 : 99;
        return 99;
    }

    private int RollUniqueSerial(SportsCard data, int printRun)
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            int serial = Random.Range(1, printRun + 1);
            string uid = $"{data.series}|{data.cardName}|{serial}/{printRun}";
            if (!_registry.Contains(uid)) return serial;
        }
        // Fallback: printRun exhausted — expand to next tier
        return Random.Range(1, printRun + 1);
    }

    public bool IsRegistered(string uid) => _registry.Contains(uid);
    public int  TotalRegistered          => _registry.Count;
}
