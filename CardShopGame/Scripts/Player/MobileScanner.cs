using TMPro;
using UnityEngine;

/// <summary>
/// Hold RMB to raise phone HUD. Raycasts at cards and overlays
/// name, market value, and pop-report rarity in the viewfinder.
/// </summary>
public class MobileScanner : MonoBehaviour
{
    [Header("Raycast")]
    public Camera   firstPersonCamera;
    public float    maxScanDistance = 2.5f;
    public LayerMask cardLayer;

    [Header("HUD")]
    public GameObject          scannerOverlay;
    public TextMeshProUGUI     uiCardName;
    public TextMeshProUGUI     uiMarketValue;
    public TextMeshProUGUI     uiRarityMetrics;

    private bool _active;

    // ----------------------------------------------------------------

    private void Update()
    {
        if (Input.GetKey(KeyCode.Mouse1))
            Scan();
        else if (_active)
            Deactivate();
    }

    private void Scan()
    {
        if (!_active)
        {
            _active = true;
            if (scannerOverlay) scannerOverlay.SetActive(true);
        }

        var ray = firstPersonCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out var hit, maxScanDistance, cardLayer))
        {
            var viz = hit.collider.GetComponent<CardVisualizer>();
            if (viz?.CurrentCard != null)
            {
                var c = viz.CurrentCard;
                if (uiCardName)      uiCardName.text      = c.data.cardName;
                if (uiMarketValue)   uiMarketValue.text   = $"Est. Value: ${c.GetSalePrice():F2}";
                if (uiRarityMetrics) uiRarityMetrics.text =
                    $"Tier: {c.data.tier}  |  Serial: {c.serialNumber}  |  " +
                    (c.isGraded ? $"Grade: {c.psaGrade:F1}" : "Ungraded");
                return;
            }
        }

        if (uiCardName)      uiCardName.text      = "Scanning…";
        if (uiMarketValue)   uiMarketValue.text   = "";
        if (uiRarityMetrics) uiRarityMetrics.text = "Point at a card";
    }

    private void Deactivate()
    {
        _active = false;
        if (scannerOverlay) scannerOverlay.SetActive(false);
    }
}
