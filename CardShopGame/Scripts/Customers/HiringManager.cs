using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages employee roster and deducts hourly wages from shop funds.
/// Accessible from the Tablet Staff app.
/// </summary>
public class HiringManager : MonoBehaviour
{
    public static HiringManager Instance { get; private set; }

    [Header("Hire-able Employee Prefabs")]
    public GameObject cashierPrefab;
    public GameObject stockerPrefab;
    public Transform  employeeSpawnPoint;

    private readonly List<EmployeeAI> _staff = new();
    private float _wageTimer;
    private const float WageIntervalSeconds = 30f;   // pay every game-hour equivalent

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        _wageTimer += Time.deltaTime;
        if (_wageTimer >= WageIntervalSeconds)
        {
            _wageTimer = 0f;
            PayWages();
        }
    }

    // ----------------------------------------------------------------

    public void RegisterEmployee(EmployeeAI emp)   => _staff.Add(emp);
    public void UnregisterEmployee(EmployeeAI emp) => _staff.Remove(emp);

    public bool HireCashier() => Hire(cashierPrefab, "Cashier");
    public bool HireStocker() => Hire(stockerPrefab, "Stocker");

    private bool Hire(GameObject prefab, string title)
    {
        if (prefab == null || employeeSpawnPoint == null) return false;
        Instantiate(prefab, employeeSpawnPoint.position, Quaternion.identity);
        NotificationSystem.Show($"👷 {title} hired!");
        return true;
    }

    public void FireEmployee(EmployeeAI emp)
    {
        _staff.Remove(emp);
        Destroy(emp.gameObject);
        NotificationSystem.Show("Employee let go.");
    }

    private void PayWages()
    {
        float total = 0f;
        foreach (var emp in _staff) total += emp.hourlyWage;
        if (total <= 0) return;

        ShopManager.Instance.SpendFunds(total);
        NotificationSystem.Show($"💸 Wages paid: ${total:F2}");
    }

    public int StaffCount => _staff.Count;
}
