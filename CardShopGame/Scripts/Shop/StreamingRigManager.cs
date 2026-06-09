using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Manages the in-store streaming rig and wall monitor feeds.
/// Viewer count is seeded from MarketingManager hype and ticks up on exciting events.
/// </summary>
public class StreamingRigManager : MonoBehaviour
{
    public static StreamingRigManager Instance { get; private set; }

    [Header("Analytics")]
    public int  currentLiveViewers;
    public int  totalChannelFollowers = 1200;
    public bool IsLive { get; private set; }

    [Header("In-World Monitors")]
    public TextMeshProUGUI storeMonitorChat;
    public TextMeshProUGUI storeMonitorViewerCount;

    private static readonly string[] ChatLines =
    {
        "OMG CHIP IT!", "PSA 10 OVER HERE!", "HUGE HIT!", "No way...",
        "SHEESH 🔥", "LFG!!!", "Is that a 1/1???", "PACK THE WAX!",
    };

    private Coroutine _chatRoutine;

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ----------------------------------------------------------------

    public void StartBroadcast()
    {
        if (IsLive) return;
        IsLive = true;

        float hype = MarketingManager.Instance ? MarketingManager.Instance.HypeMultiplier : 1f;
        currentLiveViewers = Mathf.FloorToInt(Random.Range(50f, 150f) * hype);

        _chatRoutine = StartCoroutine(ChatSimRoutine());
        NotificationSystem.Show("🔴 LIVE — stream started! Chat is watching.");
        AudioManager.Play("stream_start_jingle");
    }

    public void StopBroadcast()
    {
        if (!IsLive) return;
        IsLive = false;
        if (_chatRoutine != null) StopCoroutine(_chatRoutine);
        currentLiveViewers = 0;
        if (storeMonitorViewerCount) storeMonitorViewerCount.text = "OFFLINE";
    }

    /// <summary>Called externally (e.g. PackHypeController) on big pulls.</summary>
    public void SpikeChatOnHit(int viewerBump)
    {
        if (!IsLive) return;
        currentLiveViewers += viewerBump;
        totalChannelFollowers += Random.Range(1, viewerBump / 2 + 1);
    }

    // ----------------------------------------------------------------

    private IEnumerator ChatSimRoutine()
    {
        while (IsLive)
        {
            yield return new WaitForSeconds(Random.Range(0.5f, 2f));

            string line = $"Collector_{Random.Range(10, 99)}: {ChatLines[Random.Range(0, ChatLines.Length)]}";
            if (storeMonitorChat)
                storeMonitorChat.text = line + "\n" + storeMonitorChat.text;

            if (storeMonitorViewerCount)
                storeMonitorViewerCount.text = $"🔴 LIVE: {currentLiveViewers:N0} viewers";

            if (Random.value > 0.85f)
                currentLiveViewers += Random.Range(5, 15);
        }
    }
}
