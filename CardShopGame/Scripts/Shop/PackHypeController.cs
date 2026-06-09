using System.Collections;
using UnityEngine;

/// <summary>
/// Tracks consecutive rare pulls and builds a hype combo multiplier.
/// Big hits trigger screen-shake, streaming chat UI explosions, and
/// an immediate foot-traffic surge via MarketingManager.
/// </summary>
public class PackHypeController : MonoBehaviour
{
    public static PackHypeController Instance { get; private set; }

    [Header("Combo Settings")]
    public float ComboMultiplier    { get; private set; } = 1f;
    public float comboDurationSecs  = 7f;

    [Header("Camera Shake")]
    public Transform cameraTransform;
    public float     shakeStrength  = 0.22f;
    public float     shakeDuration  = 0.38f;

    private float _comboTimer;
    private Coroutine _shake;

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (_comboTimer > 0)
        {
            _comboTimer -= Time.deltaTime;
            if (_comboTimer <= 0f)
            {
                ComboMultiplier = 1f;
                NotificationSystem.Show("Combo reset.");
            }
        }
    }

    // ----------------------------------------------------------------

    /// <summary>Call after every card pull from a pack.</summary>
    public void RegisterPull(CardTier tier)
    {
        _comboTimer = comboDurationSecs;

        switch (tier)
        {
            case CardTier.Immortal:
                ComboMultiplier += 3.0f;
                TriggerShake(shakeDuration, shakeStrength * 2);
                NotificationSystem.Show("🔥🔥🔥 IMMORTAL PULL!!! Chat is going crazy!");
                MarketingManager.Instance?.PurchaseCampaign(CampaignType.TradeNight);  // instant hype
                AudioManager.Play("big_pull");
                break;

            case CardTier.PatchAuto:
            case CardTier.Autograph:
                ComboMultiplier += 1.5f;
                TriggerShake(shakeDuration, shakeStrength);
                NotificationSystem.Show($"🎉 HIT! x{ComboMultiplier:F1} combo!");
                AudioManager.Play("big_pull");
                break;

            case CardTier.Kaboom:
            case CardTier.Downtown:
                ComboMultiplier += 0.8f;
                NotificationSystem.Show($"⚡ Rare pull! x{ComboMultiplier:F1}");
                break;

            default:
                // Base / Insert — no combo gain but keep timer alive
                break;
        }

        ReputationManager.Instance?.RecordPositiveEvent(ComboMultiplier * 0.5f);
    }

    private void TriggerShake(float duration, float magnitude)
    {
        if (_shake != null) StopCoroutine(_shake);
        if (cameraTransform) _shake = StartCoroutine(Shake(duration, magnitude));
    }

    private IEnumerator Shake(float dur, float mag)
    {
        Vector3 origin = cameraTransform.localPosition;
        float elapsed  = 0f;
        while (elapsed < dur)
        {
            float x = Random.Range(-1f, 1f) * mag;
            float y = Random.Range(-1f, 1f) * mag;
            cameraTransform.localPosition = new Vector3(origin.x + x, origin.y + y, origin.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cameraTransform.localPosition = origin;
    }
}
