using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player's personal binder — cards here are NOT for sale.
/// Completing defined sets awards permanent store reputation bonuses.
/// </summary>
public class PersonalCollectionManager : MonoBehaviour
{
    public static PersonalCollectionManager Instance { get; private set; }

    public IReadOnlyList<CardInstance> PersonalBinder => _binder;
    private readonly List<CardInstance> _binder = new();

    [Header("Set Achievements")]
    public List<SetAchievement> trackedSets;

    [Serializable]
    public class SetAchievement
    {
        public string       setName;
        public List<SportsCard> requiredCards;
        public float        reputationBonus = 5f;
        public bool         isCompleted;
    }

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ----------------------------------------------------------------

    public void AddToBinder(CardInstance card)
    {
        InventoryManager.Instance.RemoveFromBackRoom(card);
        _binder.Add(card);
        NotificationSystem.Show($"📘 {card.GetDisplayName()} added to personal binder.");
        CheckSetProgress();
    }

    public void RemoveFromBinder(CardInstance card)
    {
        _binder.Remove(card);
        InventoryManager.Instance.AddToBackRoom(card);
    }

    private void CheckSetProgress()
    {
        foreach (var set in trackedSets)
        {
            if (set.isCompleted) continue;

            bool complete = true;
            foreach (var req in set.requiredCards)
            {
                if (!_binder.Exists(c => c.data == req)) { complete = false; break; }
            }

            if (complete)
            {
                set.isCompleted = true;
                MarketingManager.Instance.HypeMultiplier += set.reputationBonus / 100f;
                NotificationSystem.Show($"🏆 SET COMPLETE: {set.setName}! +{set.reputationBonus} reputation.");
                AudioManager.Play("upgrade_fanfare");
            }
        }
    }
}
