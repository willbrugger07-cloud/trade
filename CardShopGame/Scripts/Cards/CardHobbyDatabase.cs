using UnityEngine;

namespace SportsCardSimulator
{
    // ----------------------------------------------------------------
    // Enums

    public static class HobbyDataTiers
    {
        public enum ChaseCategory
        {
            None,
            JerseySwatch,
            PrimePatch,
            ShieldLogoman,
            StickerAuto,
            OnCardInk,
            DualInkBooklet,
        }

        public enum CardPrintStyle
        {
            TraditionalPaper,
            ChromiumFoil,
            OpticLaser,
            AcetateClear,
            FlawlessGilded,
        }

        public enum BaseRarity
        {
            CommonBase,
            ShortPrintRookie,
            SuperShortPrint,
        }
    }

    // ----------------------------------------------------------------

    [System.Serializable]
    public class CardVisualIdentity
    {
        public string parallelName = "Standard Base";
        public HobbyDataTiers.CardPrintStyle printFinish = HobbyDataTiers.CardPrintStyle.TraditionalPaper;

        public int  serialCurrentIndex = 0;
        public int  serialMaxPrintRun  = 0;
        public bool IsNumbered => serialMaxPrintRun > 0;
        public bool IsOneOfOne => serialMaxPrintRun == 1;

        public HobbyDataTiers.ChaseCategory hitType    = HobbyDataTiers.ChaseCategory.None;
        public string                        autoInkColor = "None";
        public bool                          isDieCutShape;

        public Color primaryFoilTint      = Color.white;
        [Range(0f, 1f)] public float holographicIntensity;
    }

    // ----------------------------------------------------------------

    [System.Serializable]
    public class GeneratedSportsCard
    {
        public string athleteName;
        public string teamCity;
        public int    rosterYear;
        public string subsetLabel;

        public CardVisualIdentity visualProfile = new();
        public int   conditionRawScore    = 10;
        public float customRetailStickerPrice;

        public float CalculateBaseMarketValue(float athleteBaseMarketWorth)
        {
            float v = athleteBaseMarketWorth;

            v *= visualProfile.printFinish switch
            {
                HobbyDataTiers.CardPrintStyle.ChromiumFoil   => 1.25f,
                HobbyDataTiers.CardPrintStyle.OpticLaser     => 1.50f,
                HobbyDataTiers.CardPrintStyle.AcetateClear   => 2.00f,
                HobbyDataTiers.CardPrintStyle.FlawlessGilded => 5.00f,
                _                                            => 1.00f,
            };

            switch (visualProfile.hitType)
            {
                case HobbyDataTiers.ChaseCategory.StickerAuto:
                    v += 50f;
                    break;
                case HobbyDataTiers.ChaseCategory.OnCardInk:
                    v = v * 1.5f + 150f;
                    break;
                case HobbyDataTiers.ChaseCategory.PrimePatch:
                    v = v * 1.3f + 100f;
                    break;
                case HobbyDataTiers.ChaseCategory.ShieldLogoman:
                    v = v * 3.0f + 500f;
                    break;
                case HobbyDataTiers.ChaseCategory.DualInkBooklet:
                    v = v * 2.0f + 300f;
                    break;
            }

            if (visualProfile.IsNumbered)
            {
                v *= visualProfile.serialMaxPrintRun switch
                {
                    99 => 1.75f,
                    25 => 3.00f,
                    5  => 6.00f,
                    1  => 30.0f,
                    _  => 1.00f,
                };
            }

            if (visualProfile.isDieCutShape) v *= 1.2f;

            return Mathf.Round(v * 100f) / 100f;
        }
    }
}
