using UnityEngine;

/// <summary>
/// In-game tablet — press Tab to open/close.
/// Houses the Market app, Wholesale ordering app, and Staff hiring app.
/// Pauses player look/move while open via PlayerController.UIOpen flag.
/// </summary>
public class PersonalTablet : MonoBehaviour
{
    [Header("Apps (child panels)")]
    public GameObject tabletRoot;
    public GameObject marketAppPanel;
    public GameObject wholesaleAppPanel;
    public GameObject staffAppPanel;
    public GameObject ledgerAppPanel;

    private bool _open;
    private GameObject _activeApp;

    // ----------------------------------------------------------------

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            Toggle();
    }

    private void Toggle()
    {
        _open = !_open;
        tabletRoot.SetActive(_open);
        PlayerController.UIOpen = _open;

        Cursor.lockState = _open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible   = _open;

        if (!_open) CloseAllApps();
    }

    // ---- App buttons (bind to UI buttons) --------------------------

    public void OpenMarketApp()    => SwitchApp(marketAppPanel);
    public void OpenWholesaleApp() => SwitchApp(wholesaleAppPanel);
    public void OpenStaffApp()     => SwitchApp(staffAppPanel);
    public void OpenLedgerApp()    => SwitchApp(ledgerAppPanel);

    private void SwitchApp(GameObject app)
    {
        CloseAllApps();
        _activeApp = app;
        app?.SetActive(true);
    }

    private void CloseAllApps()
    {
        marketAppPanel?.SetActive(false);
        wholesaleAppPanel?.SetActive(false);
        staffAppPanel?.SetActive(false);
        ledgerAppPanel?.SetActive(false);
        _activeApp = null;
    }
}
