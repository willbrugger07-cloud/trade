using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles PSA/BGS-style card grading submissions.
/// Cards spend N in-game days in the pipeline, then return with a numeric grade.
/// </summary>
public class GradingManager : MonoBehaviour
{
    public static GradingManager Instance { get; private set; }

    [Header("Grading Economics")]
    public float gradingFee     = 25f;
    public int   daysToReturn   = 3;

    private readonly List<PendingSubmission> _pipeline = new();

    private struct PendingSubmission
    {
        public CardInstance card;
        public int          returnDay;
    }

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()  => DayCycleManager.OnNewDay += CheckReturns;
    private void OnDisable() => DayCycleManager.OnNewDay -= CheckReturns;

    // ----------------------------------------------------------------

    /// <summary>
    /// Submit a raw card for grading. Deducts the fee immediately;
    /// card returns to back-room inventory after daysToReturn game days.
    /// </summary>
    public bool SubmitCard(CardInstance card)
    {
        if (card.isGradingPending || card.isGraded)
        {
            NotificationSystem.Show("Card is already graded or pending.");
            return false;
        }
        if (ShopManager.Instance.shopFunds < gradingFee)
        {
            NotificationSystem.Show("Not enough funds to pay grading fee.");
            return false;
        }

        ShopManager.Instance.shopFunds -= gradingFee;
        card.isGradingPending           = true;
        card.gradingReturnDay           = DayCycleManager.Instance.CurrentDay + daysToReturn;

        _pipeline.Add(new PendingSubmission
        {
            card      = card,
            returnDay = card.gradingReturnDay,
        });

        NotificationSystem.Show($"{card.data.cardName} sent to grading. Returns Day {card.gradingReturnDay}.");
        AudioManager.Play("grading_submit");
        return true;
    }

    private void CheckReturns()
    {
        int today = DayCycleManager.Instance.CurrentDay;

        for (int i = _pipeline.Count - 1; i >= 0; i--)
        {
            var sub = _pipeline[i];
            if (sub.returnDay > today) continue;

            float grade = RollGrade();
            sub.card.psaGrade         = grade;
            sub.card.isGradingPending = false;

            InventoryManager.Instance.AddToBackRoom(sub.card);
            _pipeline.RemoveAt(i);

            string result = grade >= 10f ? "💎 GEM MINT 10!" : $"Grade {grade:F1}";
            NotificationSystem.Show($"{sub.card.data.cardName} returned — PSA {result}");
            AudioManager.Play("grading_return");
        }
    }

    // Weighted distribution — skewed toward 8–9 like real grading
    private float RollGrade()
    {
        float r = Random.value * 100f;
        if (r < 5f)  return 10f;
        if (r < 18f) return 9.5f;
        if (r < 40f) return 9f;
        if (r < 62f) return 8.5f;
        if (r < 78f) return 8f;
        if (r < 88f) return 7f;
        if (r < 94f) return 6f;
        return Random.Range(1, 6);
    }
}
