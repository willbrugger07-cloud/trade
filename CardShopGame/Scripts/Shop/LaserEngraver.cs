using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Laser engraving desk. Accepts a graded card slab, lets the player
/// choose a cosmetic design, then burns a permanent visual upgrade that
/// boosts the card's sale value for premium collectors.
/// </summary>
public class LaserEngraver : MonoBehaviour, IInteractable
{
    [Serializable]
    public class SlabCosmetics
    {
        public string customLabelName;
        public Color  neonBorderColor = Color.cyan;
        public bool   hasGoldEtching;
        public float  premiumValueMultiplier = 1f;
    }

    [Header("State")]
    public CardInstance activeSlabOnPad;
    public SlabCosmetics currentDesign = new();

    [Header("Settings")]
    public float engravingTimeSecs = 5f;

    [Header("FX")]
    public ParticleSystem laserSparkParticles;

    public bool IsEngraving { get; private set; }

    // ----------------------------------------------------------------

    public void Interact(PlayerInteraction player) =>
        LaserEngraverUI.Instance?.Open(this);

    public void LoadSlab(CardInstance gradedCard)
    {
        if (!gradedCard.isGraded)
        {
            NotificationSystem.Show("Only graded slabs can be engraved.");
            return;
        }
        activeSlabOnPad = gradedCard;
        NotificationSystem.Show($"Loaded {gradedCard.data.cardName} onto engraving pad.");
    }

    public void ConfigureDesign(string label, Color color, bool goldFrame)
    {
        currentDesign = new SlabCosmetics
        {
            customLabelName       = label,
            neonBorderColor       = color,
            hasGoldEtching        = goldFrame,
            premiumValueMultiplier = goldFrame ? 1.50f : 1.25f,
        };
    }

    public void StartEngravingSequence()
    {
        if (activeSlabOnPad == null || IsEngraving) return;
        StartCoroutine(EngraveRoutine());
    }

    private IEnumerator EngraveRoutine()
    {
        IsEngraving = true;
        laserSparkParticles?.Play();
        AudioManager.Play("laser_hum");

        yield return new WaitForSeconds(engravingTimeSecs);

        laserSparkParticles?.Stop();

        activeSlabOnPad.slabCosmetics      = currentDesign;
        activeSlabOnPad.overridePrice      =
            activeSlabOnPad.GetSalePrice() * currentDesign.premiumValueMultiplier;

        NotificationSystem.Show($"✨ Engraving complete! {activeSlabOnPad.data.cardName} upgraded — " +
                                $"{currentDesign.premiumValueMultiplier * 100f - 100f:F0}% value bonus.");
        AudioManager.Play("laser_done");
        IsEngraving    = false;
        activeSlabOnPad = null;
    }
}
