using TMPro;
using UnityEngine;

/// <summary>
/// Stores the shop's custom name and projects it onto the outdoor 3D sign
/// mesh and the register receipt text element.
/// </summary>
public class StoreFrontManager : MonoBehaviour
{
    public static StoreFrontManager Instance { get; private set; }

    public string storeName = "My Card Shop";

    [Header("In-World References")]
    public TextMeshPro outdoorSignText;
    public TextMeshPro receiptHeaderText;

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start() => Apply();

    // ----------------------------------------------------------------

    public void SetStoreName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return;
        storeName = newName.Trim();
        Apply();
        NotificationSystem.Show($"🏪 Store rebranded: {storeName}");
    }

    private void Apply()
    {
        if (outdoorSignText)    outdoorSignText.text    = storeName.ToUpper();
        if (receiptHeaderText)  receiptHeaderText.text  = storeName;
    }
}
