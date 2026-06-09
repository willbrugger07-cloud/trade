using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UtilityBill
{
    public string billName;
    public float  invoiceAmount;
    public bool   isPaid;
}

/// <summary>
/// Generates daily invoices (rent, power, etc.) and applies late-payment
/// penalties if bills are unpaid at midnight.
/// </summary>
public class BusinessBillingSystem : MonoBehaviour
{
    public static BusinessBillingSystem Instance { get; private set; }

    public List<UtilityBill> bills = new();

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        DayCycleManager.OnNewDay    += GenerateInvoices;
        DayCycleManager.OnShopClose += ProcessMidnightArrears;
    }

    private void OnDisable()
    {
        DayCycleManager.OnNewDay    -= GenerateInvoices;
        DayCycleManager.OnShopClose -= ProcessMidnightArrears;
    }

    // ----------------------------------------------------------------

    private void GenerateInvoices()
    {
        bills.Clear();

        int level = StoreLayoutManager.Instance ? StoreLayoutManager.Instance.currentLayoutLevel : 0;
        float power = Mathf.Round((12.50f + level * 5.50f) * 100f) / 100f;

        bills.Add(new UtilityBill { billName = "Commercial Property Rent", invoiceAmount = 45.00f });
        bills.Add(new UtilityBill { billName = "Municipal Power",          invoiceAmount = power  });

        NotificationSystem.Show($"📋 {bills.Count} bills due today. Total: ${TotalDue():F2}");
    }

    public void PayBill(int index)
    {
        if (index < 0 || index >= bills.Count) return;
        var bill = bills[index];
        if (bill.isPaid) return;

        if (!ShopManager.Instance.SpendFunds(bill.invoiceAmount, bill.billName)) return;
        bill.isPaid = true;
        NotificationSystem.Show($"✅ Paid: {bill.billName} (${bill.invoiceAmount:F2})");
    }

    private void ProcessMidnightArrears()
    {
        foreach (var bill in bills)
        {
            if (bill.isPaid) continue;
            float penalty = Mathf.Round(bill.invoiceAmount * 1.15f * 100f) / 100f;
            ShopManager.Instance.SpendFunds(penalty);
            ReputationManager.Instance?.RecordNegativeEvent(3f);
            NotificationSystem.Show($"⚠️ Late payment penalty: {bill.billName} -${penalty:F2}");
        }
    }

    public float TotalDue() =>
        bills.FindAll(b => !b.isPaid).ConvertAll(b => b.invoiceAmount).Sum();
}
