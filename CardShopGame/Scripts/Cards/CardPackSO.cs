using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Card Pack", menuName = "Card Shop/Card Pack")]
public class CardPackSO : ScriptableObject
{
    [Header("Pack Identity")]
    public string  packName;       // e.g., "Prizm Hobby Box"
    public Sprite  packArtwork;
    public int     cardsPerPack = 5;
    public float   wholesaleCost;  // what the shop owner pays

    [Header("Drop Table")]
    public List<CardDrop> dropTable;

    [Serializable]
    public struct CardDrop
    {
        public SportsCard cardData;
        [Range(0f, 100f)] public float pullChance;
    }

    // Guaranteed hit slots (e.g., every hobby pack has 1 auto)
    [Header("Guaranteed Slots")]
    public List<GuaranteedSlot> guaranteedSlots;

    [Serializable]
    public struct GuaranteedSlot
    {
        public CardTier minimumTier;
        public List<SportsCard> eligibleCards;
    }

    // ----------------------------------------------------------------

    public List<CardInstance> OpenPack()
    {
        var pulled = new List<CardInstance>();

        // Fill guaranteed slots first
        foreach (var slot in guaranteedSlots)
        {
            var eligible = slot.eligibleCards
                .FindAll(c => c.tier >= slot.minimumTier);
            if (eligible.Count > 0)
            {
                var card = eligible[UnityEngine.Random.Range(0, eligible.Count)];
                pulled.Add(new CardInstance(card, RollCondition()));
            }
        }

        // Fill remaining slots from the drop table
        int remaining = cardsPerPack - pulled.Count;
        for (int i = 0; i < remaining; i++)
        {
            var card = RollDropTable();
            if (card != null)
                pulled.Add(new CardInstance(card, RollCondition()));
        }

        return pulled;
    }

    private SportsCard RollDropTable()
    {
        float roll = UnityEngine.Random.Range(0f, 100f);
        float cumulative = 0f;
        foreach (var drop in dropTable)
        {
            cumulative += drop.pullChance;
            if (roll <= cumulative) return drop.cardData;
        }
        return dropTable.Count > 0 ? dropTable[0].cardData : null;
    }

    private CardCondition RollCondition()
    {
        float r = UnityEngine.Random.value;
        if (r < 0.02f) return CardCondition.GemMint;
        if (r < 0.25f) return CardCondition.NearMint;
        if (r < 0.60f) return CardCondition.Excellent;
        if (r < 0.82f) return CardCondition.VeryGood;
        if (r < 0.95f) return CardCondition.Good;
        return CardCondition.Poor;
    }
}
