using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Master HUD controller. Subscribes to DayCycleManager and ReputationManager
/// events and pushes live data to every canvas element each frame / on event.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // ----------------------------------------------------------------
    // Inspector references

    [Header("HUD – Top Bar")]
    public TextMeshProUGUI clockText;
    public TextMeshProUGUI fundsText;
    public TextMeshProUGUI reputationText;
    public TextMeshProUGUI dayLabel;

    [Header("Store TV Ticker")]
    public TextMeshProUGUI   tvTickerText;
    public RectTransform     tvTickerRect;
    public float             tickerScrollSpeed = 60f;

    [Header("End-of-Day Panel")]
    public GameObject        eodPanel;
    public TextMeshProUGUI   eodDayLabel;
    public TextMeshProUGUI   eodRevenueText;
    public TextMeshProUGUI   eodExpensesText;
    public TextMeshProUGUI   eodNetText;
    public TextMeshProUGUI   eodRepText;
    public Button            eodConfirmButton;

    [Header("Notification Banner")]
    public TextMeshProUGUI   notifText;
    public CanvasGroup       notifGroup;

    // ----------------------------------------------------------------

    private float  _tickerStartX;
    private float  _tickerWidth;
    private string _tickerFull = "";

    private Coroutine _notifRoutine;

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        DayCycleManager.OnNewDay     += HandleNewDay;
        DayCycleManager.OnShopOpen   += HandleShopOpen;
        DayCycleManager.OnShopClose  += HandleShopClose;

        if (eodConfirmButton) eodConfirmButton.onClick.AddListener(OnEodConfirm);

        if (tvTickerRect)
        {
            _tickerStartX = tvTickerRect.anchoredPosition.x;
            _tickerWidth  = tvTickerRect.rect.width;
        }

        NotificationSystem.OnNotification += ShowBanner;
    }

    private void OnDisable()
    {
        DayCycleManager.OnNewDay    -= HandleNewDay;
        DayCycleManager.OnShopOpen  -= HandleShopOpen;
        DayCycleManager.OnShopClose -= HandleShopClose;

        NotificationSystem.OnNotification -= ShowBanner;
    }

    // ----------------------------------------------------------------

    private void Update()
    {
        RefreshHUD();
        ScrollTicker();
    }

    // ----------------------------------------------------------------
    // HUD live refresh

    private void RefreshHUD()
    {
        if (clockText && DayCycleManager.Instance)
            clockText.text = DayCycleManager.Instance.GetTimeString();

        if (fundsText && ShopManager.Instance)
            fundsText.text = $"${ShopManager.Instance.shopFunds:N2}";

        if (reputationText && ReputationManager.Instance)
            reputationText.text = $"Rep {ReputationManager.Instance.ReputationScore:F0}/100";
    }

    // ----------------------------------------------------------------
    // TV Ticker

    private void ScrollTicker()
    {
        if (!tvTickerRect || string.IsNullOrEmpty(_tickerFull)) return;

        Vector2 pos = tvTickerRect.anchoredPosition;
        pos.x -= tickerScrollSpeed * Time.deltaTime;

        if (pos.x < -_tickerWidth)
            pos.x = _tickerStartX;

        tvTickerRect.anchoredPosition = pos;
    }

    public void PushTickerLine(string line)
    {
        _tickerFull += $"   ▶   {line}";
        if (tvTickerText) tvTickerText.text = _tickerFull;
    }

    // ----------------------------------------------------------------
    // Event handlers

    private void HandleNewDay()
    {
        if (dayLabel && DayCycleManager.Instance)
            dayLabel.text = $"Day {DayCycleManager.Instance.CurrentDay}";
    }

    private void HandleShopOpen()
    {
        if (eodPanel) eodPanel.SetActive(false);
    }

    private void HandleShopClose()
    {
        ShowEndOfDayPanel();
    }

    // ----------------------------------------------------------------
    // End-of-day popup

    private void ShowEndOfDayPanel()
    {
        if (!eodPanel) return;

        var dc = DayCycleManager.Instance;
        var sm = ShopManager.Instance;
        var rm = ReputationManager.Instance;

        float revenue  = (sm != null) ? sm.DailyRevenue  : (dc != null ? dc.DailyRevenue  : 0f);
        float expenses = dc != null ? dc.DailyExpenses : 0f;
        float net      = revenue - expenses;

        if (eodDayLabel)   eodDayLabel.text   = dc ? $"Day {dc.CurrentDay} Summary" : "Summary";
        if (eodRevenueText) eodRevenueText.text = $"Revenue   ${revenue:N2}";
        if (eodExpensesText) eodExpensesText.text = $"Expenses  -${expenses:N2}";
        if (eodNetText)
        {
            eodNetText.text  = $"Net  ${net:N2}";
            eodNetText.color = net >= 0f ? Color.green : Color.red;
        }
        if (eodRepText && rm) eodRepText.text = $"Reputation  {rm.ReputationScore:F0} / 100";
    }

    /// <summary>Called by ReputationManager.Modify() so the HUD stays in sync.</summary>
    public void RefreshReputation(float score)
    {
        if (reputationText) reputationText.text = $"Rep {score:F0}/100";
        if (reputationText) reputationText.color = score < 25f ? Color.red
                                                  : score < 50f ? Color.yellow
                                                  : Color.white;
    }

    private void OnEodConfirm()
    {
        if (eodPanel) eodPanel.SetActive(false);
        DayCycleManager.Instance?.StartNewDay();
    }

    // ----------------------------------------------------------------
    // Notification banner

    public void ShowBanner(string message)
    {
        if (!notifText) return;
        if (_notifRoutine != null) StopCoroutine(_notifRoutine);
        _notifRoutine = StartCoroutine(BannerRoutine(message));
    }

    private IEnumerator BannerRoutine(string message)
    {
        notifText.text = message;
        if (notifGroup) notifGroup.alpha = 1f;

        yield return new WaitForSeconds(3f);

        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            if (notifGroup) notifGroup.alpha = 1f - (t / 0.5f);
            yield return null;
        }

        if (notifGroup) notifGroup.alpha = 0f;
    }
}
