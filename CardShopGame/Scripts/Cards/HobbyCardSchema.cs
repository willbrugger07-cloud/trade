using UnityEngine;

namespace SportsCardTycoon.Core
{
    public static class HobbyEnums
    {
        public enum SportType { Basketball, Football, Baseball, Hockey }

        public enum CardStockType
        {
            VintagePaper,
            ChromiumFoil,
            OpticLaser,
            AcetateClear,
            GildedTimber,
        }

        public enum ParallelPattern
        {
            BasePlain,
            RefractorSilver,
            WaveShimmer,
            MojoDiamond,
            ZebraStripe,
            GenesisNebula,
        }

        public enum HitCategory
        {
            None,
            PlayerWornPatch,
            PrimeMultiColor,
            SlabbedLogoman,
            StickerSignature,
            OnCardInk,
            DualInkBooklet,
        }
    }

    [System.Serializable]
    public class CardVisualProfile
    {
        public HobbyEnums.CardStockType   stockMaterial   = HobbyEnums.CardStockType.VintagePaper;
        public HobbyEnums.ParallelPattern patternOverlay  = HobbyEnums.ParallelPattern.BasePlain;
        public Color                      parallelColorTint = Color.white;
        public bool                       isDieCut;

        public int  currentSerialIndex = 0;
        public int  maxPrintRun        = 0;
        public bool IsNumbered         => maxPrintRun > 0;
        public bool IsOneOfOne         => maxPrintRun == 1;

        public HobbyEnums.HitCategory cardHitType   = HobbyEnums.HitCategory.None;
        public string                  inkColorHex  = "#000000";
    }

    [System.Serializable]
    public class LiveCardInstance
    {
        public string                  athleteName;
        public string                  teamDesignation;
        public int                     releaseYear;
        public HobbyEnums.SportType    sport;
        public string                  cardSetLabel;
        public CardVisualProfile       visualAttributes = new();
        public int                     rawConditionScore   = 10;
        public float                   dynamicMarketValue;
        public float                   playerAssignedPrice;

        public float CompileMarketValue(float playerBaselineWorth)
        {
            float v = playerBaselineWorth;

            v *= visualAttributes.stockMaterial switch
            {
                HobbyEnums.CardStockType.ChromiumFoil => 1.3f,
                HobbyEnums.CardStockType.OpticLaser   => 1.6f,
                HobbyEnums.CardStockType.AcetateClear => 2.2f,
                HobbyEnums.CardStockType.GildedTimber => 5.0f,
                _                                     => 1.0f,
            };

            switch (visualAttributes.patternOverlay)
            {
                case HobbyEnums.ParallelPattern.RefractorSilver: v += 15f;        break;
                case HobbyEnums.ParallelPattern.WaveShimmer:     v *= 1.25f;      break;
                case HobbyEnums.ParallelPattern.MojoDiamond:     v *= 1.5f;       break;
                case HobbyEnums.ParallelPattern.ZebraStripe:     v *= 3.0f;       break;
                case HobbyEnums.ParallelPattern.GenesisNebula:   v *= 6.0f;       break;
            }

            if (visualAttributes.IsNumbered)
            {
                v *= visualAttributes.maxPrintRun switch
                {
                    99 => 1.50f,
                    25 => 3.50f,
                    5  => 8.00f,
                    1  => 35.0f,
                    _  => 1.00f,
                };
            }

            switch (visualAttributes.cardHitType)
            {
                case HobbyEnums.HitCategory.StickerSignature: v += 40f;                     break;
                case HobbyEnums.HitCategory.OnCardInk:        v = v * 1.4f + 120f;          break;
                case HobbyEnums.HitCategory.PlayerWornPatch:  v += 35f;                     break;
                case HobbyEnums.HitCategory.PrimeMultiColor:  v = v * 1.25f + 85f;         break;
                case HobbyEnums.HitCategory.SlabbedLogoman:   v = v * 4.0f + 600f;         break;
                case HobbyEnums.HitCategory.DualInkBooklet:   v = v * 2.0f + 250f;         break;
            }

            if (visualAttributes.isDieCut) v *= 1.15f;

            if (rawConditionScore < 9)
                v *= rawConditionScore / 10f;

            return Mathf.Round(v * 100f) / 100f;
        }
    }
}
