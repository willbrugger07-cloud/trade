using System;
using UnityEngine;

public enum CardCondition { Poor = 1, Good, VeryGood, Excellent, NearMint, GemMint }

/// <summary>
/// A specific physical card — wraps a SportsCard ScriptableObject with
/// runtime state: condition, serial number, PSA grade, grading status,
/// visual layout, and autograph / slab cosmetic data.
/// </summary>
[Serializable]
public class CardInstance
{
    public SportsCard data;
    public CardCondition condition;
    public string serialNumber;          // null unless numbered

    // Grading
    public bool  isGraded;
    public float psaGrade = -1f;         // -1 = ungraded
    public bool  isGradingPending;
    public int   gradingReturnDay;

    // Autograph & visual
    public bool             isAutographed;
    public CardVisualLayout layout;

    // Pricing overrides
    public float overridePrice;          // 0 = use GetSalePrice()

    // Slab customisation (LaserEngraver)
    public LaserEngraver.SlabCosmetics slabCosmetics;

    private static int _nextId = 1;
    public int instanceId;

    public CardInstance() { instanceId = _nextId++; }

    public CardInstance(SportsCard data,
                        CardCondition condition = CardCondition.NearMint)
    {
        this.data      = data;
        this.condition = condition;
        instanceId     = _nextId++;

        if (data != null && data.isNumbered)
        {
            int n = UnityEngine.Random.Range(1, data.printRun + 1);
            serialNumber = $"{n}/{data.printRun}";
        }
    }

    public float GetSalePrice()
    {
        if (overridePrice > 0f) return overridePrice;

        if (isGraded && psaGrade >= 0f)
            return data.GetGradedValue(psaGrade);

        float m = condition switch
        {
            CardCondition.GemMint   => 2.5f,
            CardCondition.NearMint  => 1.4f,
            CardCondition.Excellent => 1.0f,
            CardCondition.VeryGood  => 0.65f,
            CardCondition.Good      => 0.35f,
            _                       => 0.15f,
        };
        return data != null
            ? Mathf.Round(data.currentMarketValue * m * 100f) / 100f
            : 0f;
    }

    public string GetDisplayName()
    {
        if (data == null) return "(unknown card)";
        if (isGraded && psaGrade >= 0f) return $"PSA {psaGrade:F1} — {data.cardName}";
        if (!string.IsNullOrEmpty(serialNumber)) return $"{data.cardName} [{serialNumber}]";
        return data.cardName;
    }
}
