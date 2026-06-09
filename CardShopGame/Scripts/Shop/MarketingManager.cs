using System.Collections;
using UnityEngine;

/// <summary>
/// Spend money on ad campaigns to boost foot traffic and Whale spawn rates.
/// Accessible from the Tablet Marketing app.
/// </summary>
public class MarketingManager : MonoBehaviour
{
    public static MarketingManager Instance { get; private set; }

    public float HypeMultiplier  { get; private set; } = 1f;
    public float WhaleBonus      { get; private set; } = 0f;

    private bool _campaignActive;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ----------------------------------------------------------------

    public void PurchaseCampaign(CampaignType type)
    {
        if (_campaignActive)
        {
            NotificationSystem.Show("A campaign is already running.");
            return;
        }

        var cfg = CampaignConfig.Get(type);
        if (!ShopManager.Instance.SpendFunds(cfg.cost)) return;

        StartCoroutine(RunCampaign(cfg));
        NotificationSystem.Show($"📣 {cfg.name} campaign started! ({cfg.durationDays} days)");
        AudioManager.Play("marketing_launch");
    }

    private IEnumerator RunCampaign(CampaignConfig cfg)
    {
        _campaignActive  = true;
        HypeMultiplier   = cfg.hypeMultiplier;
        WhaleBonus       = cfg.whaleBonus;

        float seconds = cfg.durationDays * DayCycleManager.Instance.secondsPerGameHour * 24f;
        yield return new WaitForSeconds(seconds);

        HypeMultiplier  = 1f;
        WhaleBonus      = 0f;
        _campaignActive = false;
        NotificationSystem.Show("Ad campaign has ended — traffic returning to normal.");
    }
}

// ----------------------------------------------------------------

public enum CampaignType { SocialMedia, SponsorStreamer, LocalBillboard, TradeNight }

public class CampaignConfig
{
    public string name;
    public float  cost;
    public float  durationDays;
    public float  hypeMultiplier;
    public float  whaleBonus;

    public static CampaignConfig Get(CampaignType t) => t switch
    {
        CampaignType.SocialMedia      => new CampaignConfig { name="Social Media",     cost=200f,  durationDays=3f, hypeMultiplier=1.5f,  whaleBonus=0.05f },
        CampaignType.SponsorStreamer   => new CampaignConfig { name="Streamer Sponsor", cost=500f,  durationDays=2f, hypeMultiplier=1.2f,  whaleBonus=0.20f },
        CampaignType.LocalBillboard   => new CampaignConfig { name="Billboard",        cost=150f,  durationDays=7f, hypeMultiplier=1.3f,  whaleBonus=0.00f },
        CampaignType.TradeNight       => new CampaignConfig { name="Trade Night Event",cost=300f,  durationDays=1f, hypeMultiplier=2.5f,  whaleBonus=0.10f },
        _                             => new CampaignConfig { name="Unknown",          cost=0f,    durationDays=1f, hypeMultiplier=1f,    whaleBonus=0f    },
    };
}
