using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class SecurityCameraNode
{
    public string        roomName;
    public Camera        cam;
    public RenderTexture feed;
}

/// <summary>
/// Tablet CCTV app. Cycles camera feeds and lets the player trigger
/// an alarm to freeze an active shoplifter.
/// </summary>
public class SecurityCCTVSystem : MonoBehaviour
{
    public static SecurityCCTVSystem Instance { get; private set; }

    public List<SecurityCameraNode> cameras = new();
    public RawImage displayScreen;
    public GameObject tabletUI;

    private int _activeIndex;

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ----------------------------------------------------------------

    public void ToggleTablet(bool open)
    {
        if (tabletUI) tabletUI.SetActive(open);
        if (open && cameras.Count > 0) SwitchFeed(0);
    }

    public void CycleCamera(int dir)
    {
        if (cameras.Count == 0) return;
        _activeIndex = (_activeIndex + dir + cameras.Count) % cameras.Count;
        SwitchFeed(_activeIndex);
    }

    private void SwitchFeed(int index)
    {
        _activeIndex = index;
        if (displayScreen) displayScreen.texture = cameras[index].feed;
        NotificationSystem.Show($"Camera: {cameras[index].roomName}");
    }

    public void TriggerAlarm()
    {
        foreach (var shoplifter in FindObjectsOfType<ShoplifterAI>())
        {
            shoplifter.FreezeAndSurrender();
            ReputationManager.Instance?.RecordPositiveEvent(1.5f);
            NotificationSystem.Show("🚨 Alarm triggered — shoplifter caught!");
            AudioManager.Play("alarm_blare");
            return;
        }
        NotificationSystem.Show("No active shoplifters detected.");
    }
}
