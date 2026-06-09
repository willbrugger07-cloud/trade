using UnityEngine;

/// <summary>
/// Describes the physical presentation of a specific card pull:
/// set style (paper / chrome / acetate), autograph type, and patch type.
/// Drives both value calculations and the CardMeshController shader setup.
/// </summary>
[System.Serializable]
public class CardVisualLayout
{
    public enum SetStyle { PaperBase, ChromeFoil, AcetateClear, HighEndWood }
    public enum AutoType { None, StickerAuto, OnCardInk, DualAuto }
    public enum PatchType { None, JerseySwatch, PrimePatch, LogomanTag }

    [Header("Set Aesthetics")]
    public SetStyle  setType;
    public AutoType  inkStyle;
    public PatchType memorabiliaStyle;

    [Header("Visual")]
    public Color  cardBorderGlow   = Color.white;
    public string customTexturePath;

    /// <summary>
    /// Flat multiplier applied on top of the live market price to reflect
    /// the visual prestige of the card's physical materials.
    /// </summary>
    public float GetAestheticMultiplier()
    {
        float m = 1.0f;

        m += setType switch
        {
            SetStyle.ChromeFoil    => 0.25f,
            SetStyle.AcetateClear  => 0.50f,
            SetStyle.HighEndWood   => 0.75f,
            _                      => 0.00f,
        };

        m += inkStyle switch
        {
            AutoType.StickerAuto => 0.75f,
            AutoType.OnCardInk   => 1.50f,
            AutoType.DualAuto    => 2.50f,
            _                    => 0.00f,
        };

        m += memorabiliaStyle switch
        {
            PatchType.JerseySwatch => 0.50f,
            PatchType.PrimePatch   => 1.50f,
            PatchType.LogomanTag   => 5.00f,
            _                      => 0.00f,
        };

        return m;
    }
}
