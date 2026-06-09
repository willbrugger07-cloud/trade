using System;
using UnityEngine;

public enum CardCondition { Poor = 1, Good, VeryGood, Excellent, NearMint, GemMint }

/// <summary>
/// A specific physical card — wraps a SportsCard ScriptableObject with
/// runtime state: condition, serial number, PSA grade, grading status.
/// </summary>
[Serializable]
public class CardInstance
{
    public SportsCard data;
    public CardCondition condition;
    public string serialNumber;       // null unless numbered
    public float? psaGrade;           // null = ungraded
    public bool   isGradingPending;
    public int    gradingReturnDay;   // game-day the grade arrives back

    private static int _nextId = 1;
    public int instanceId;

    public CardInstance(SportsCard data,
                        CardCondition condition = CardCondition.NearMint)
    {
        this.data      = data;
        this.condition = condition;
        instanceId     = _nextId++;

        if (data.isNumbered)
        {
            int n = UnityEngine.Random.Range(1, data.printRun + 1);
            serialNumber = $"{n}/{data.printRun}";
        }
    }

    public float GetSalePrice()
    {
        if (psaGrade.HasValue)
            return data.GetGradedValue(psaGrade.Value);

        float m = condition switch
        {
            CardCondition.GemMint   => 2.5f,
            CardCondition.NearMint  => 1.4f,
            CardCondition.Excellent => 1.0f,
            CardCondition.VeryGood  => 0.65f,
            CardCondition.Good      => 0.35f,
            _                       => 0.15f,
        };
        return Mathf.Round(data.currentMarketValue * m * 100f) / 100f;
    }

    public string GetDisplayName() =>
        psaGrade.HasValue   ? $"PSA {psaGrade.Value:F1} — {data.cardName}" :
        !string.IsNullOrEmpty(serialNumber) ? $"{data.cardName} [{serialNumber}]" :
        data.cardName;
}
