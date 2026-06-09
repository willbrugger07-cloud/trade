using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Central singleton hub. Holds serialisable save state for every major
/// system and routes initialisation so sub-systems never need to find
/// each other by reference in Awake/Start ordering.
///
/// Save format: JSON written to Application.persistentDataPath/save.json
/// All currency fields are stored as integer cents to avoid float drift.
/// </summary>
public class TycoonCoreBridge : MonoBehaviour
{
    public static TycoonCoreBridge Instance { get; private set; }

    // ----------------------------------------------------------------
    // Serialisable save state

    [Serializable]
    public class CardSaveData
    {
        public string dataAssetGuid;
        public string serialNumber;
        public bool   isGraded;
        public float  psaGrade;
        public int    overridePriceCents;
        public string slabLabelName;
        public string slabBorderColorHex;
        public bool   slabHasGold;
    }

    [Serializable]
    public class SaveState
    {
        // ---- Economy ----
        public long   shopFundsCents;           // stored as cents
        public long   totalLifetimeRevenueCents;
        public float  reputationScore;

        // ---- Identity ----
        public string storeName;

        // ---- Property ----
        public int    propertyIndex;
        public bool[] propertyOwned;
        public int    subLetCount;
        public long   monthlySubLetIncomeCents;

        // ---- Insurance ----
        public int    insuranceTierIndex;

        // ---- Layout ----
        public int    layoutLevel;

        // ---- Inventory ----
        public List<CardSaveData> backRoomCards = new();
        public List<CardSaveData> vaultCards    = new();

        // ---- Collection ----
        public float  albumCompletionPct;

        // ---- Day / Calendar ----
        public int    currentDay;
        public int    dayOfWeek;

        // ---- Streaming ----
        public int    totalFollowers;
    }

    // ----------------------------------------------------------------
    // Runtime state

    public SaveState State { get; private set; } = new();

    private string SavePath =>
        Path.Combine(Application.persistentDataPath, "save.json");

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        LoadGame();
        ApplyStateToSystems();
    }

    // ----------------------------------------------------------------
    // Public API called by sub-systems on significant changes

    /// <summary>Capture a snapshot and flush to disk.</summary>
    public void SaveGame()
    {
        CaptureStateFromSystems();
        string json = JsonUtility.ToJson(State, prettyPrint: true);
        File.WriteAllText(SavePath, json);
        NotificationSystem.Show("💾 Game saved.");
    }

    public void LoadGame()
    {
        if (!File.Exists(SavePath)) return;

        try
        {
            string json = File.ReadAllText(SavePath);
            State = JsonUtility.FromJson<SaveState>(json) ?? new SaveState();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TycoonCoreBridge] Could not load save: {e.Message}");
            State = new SaveState();
        }
    }

    public void DeleteSave()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);
        State = new SaveState();
        NotificationSystem.Show("Save data deleted.");
    }

    // ----------------------------------------------------------------
    // Capture helpers — pull live values from every system

    private void CaptureStateFromSystems()
    {
        // Economy
        if (ShopManager.Instance)
        {
            State.shopFundsCents =
                (long)Math.Round(ShopManager.Instance.shopFunds * 100.0, MidpointRounding.AwayFromZero);
        }

        if (ReputationManager.Instance)
            State.reputationScore = ReputationManager.Instance.ReputationScore;

        // Identity
        if (StoreFrontManager.Instance)
            State.storeName = StoreFrontManager.Instance.storeName;

        // Property
        if (RealEstateManager.Instance)
        {
            var rem = RealEstateManager.Instance;
            State.propertyIndex  = rem.activeIndex;
            State.propertyOwned  = new bool[rem.catalog.Count];
            for (int i = 0; i < rem.catalog.Count; i++)
                State.propertyOwned[i] = rem.catalog[i].owned;
            State.subLetCount                 = rem.subLetCount;
            State.monthlySubLetIncomeCents    =
                (long)Math.Round(rem.monthlySubLetIncome * 100.0, MidpointRounding.AwayFromZero);
        }

        // Insurance
        if (SecurityVaultManager.Instance)
            State.insuranceTierIndex = (int)SecurityVaultManager.Instance.activePolicy;

        // Layout
        if (StoreLayoutManager.Instance)
            State.layoutLevel = StoreLayoutManager.Instance.currentLayoutLevel;

        // Inventory — back room
        State.backRoomCards.Clear();
        if (InventoryManager.Instance)
            foreach (var c in InventoryManager.Instance.BackRoom)
                State.backRoomCards.Add(CardToSaveData(c));

        // Day / Calendar
        if (DayCycleManager.Instance)
        {
            State.currentDay  = DayCycleManager.Instance.CurrentDay;
            State.dayOfWeek   = DayCycleManager.Instance.CurrentDayOfWeek;
        }

        // Streaming
        if (StreamingRigManager.Instance)
            State.totalFollowers = StreamingRigManager.Instance.totalChannelFollowers;
    }

    // ----------------------------------------------------------------
    // Apply helpers — push saved values back into live systems

    private void ApplyStateToSystems()
    {
        // Economy
        if (ShopManager.Instance)
            ShopManager.Instance.shopFunds =
                (float)Math.Round(State.shopFundsCents / 100.0, 2);

        if (ReputationManager.Instance)
            ReputationManager.Instance.SetScore(State.reputationScore);

        // Identity
        if (StoreFrontManager.Instance && !string.IsNullOrEmpty(State.storeName))
            StoreFrontManager.Instance.SetStoreName(State.storeName);

        // Property
        if (RealEstateManager.Instance && State.propertyOwned != null)
        {
            var rem = RealEstateManager.Instance;
            for (int i = 0; i < State.propertyOwned.Length && i < rem.catalog.Count; i++)
                rem.catalog[i].owned = State.propertyOwned[i];
            rem.activeIndex = Mathf.Clamp(State.propertyIndex, 0, rem.catalog.Count - 1);
        }

        // Insurance
        if (SecurityVaultManager.Instance)
            SecurityVaultManager.Instance.PurchaseInsurance(
                (SecurityVaultManager.InsuranceTier)State.insuranceTierIndex);

        // Day
        if (DayCycleManager.Instance)
            DayCycleManager.Instance.LoadDayState(State.currentDay, State.dayOfWeek);

        // Streaming
        if (StreamingRigManager.Instance)
            StreamingRigManager.Instance.totalChannelFollowers = State.totalFollowers;
    }

    // ----------------------------------------------------------------
    // Utility

    private static CardSaveData CardToSaveData(CardInstance c)
    {
        string colorHex = c.slabCosmetics != null
            ? "#" + ColorUtility.ToHtmlStringRGB(c.slabCosmetics.neonBorderColor)
            : "#00FFFF";

        return new CardSaveData
        {
            dataAssetGuid      = c.data ? c.data.name : "",
            serialNumber       = c.serialNumber,
            isGraded           = c.isGraded,
            psaGrade           = c.psaGrade,
            overridePriceCents =
                (int)Math.Round(c.overridePrice * 100.0, MidpointRounding.AwayFromZero),
            slabLabelName      = c.slabCosmetics?.customLabelName ?? "",
            slabBorderColorHex = colorHex,
            slabHasGold        = c.slabCosmetics?.hasGoldEtching ?? false,
        };
    }

    // ----------------------------------------------------------------
    // DayCycleManager hooks — auto-save at end of each day

    private void OnEnable()  => DayCycleManager.OnShopClose += SaveGame;
    private void OnDisable() => DayCycleManager.OnShopClose -= SaveGame;
}
