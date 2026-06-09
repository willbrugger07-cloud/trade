using System;
using System.IO;
using UnityEngine;

/// <summary>
/// JSON-based save system. Call SaveSystem.Save() / SaveSystem.Load()
/// from the ShopManager or on application quit.
/// </summary>
public static class SaveSystem
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "shopSave.json");

    [Serializable]
    public class SaveData
    {
        public float shopFunds;
        public int   currentDay;
        public float currentHour;
        public int   shopLevel;
        // Extend with serializable card lists as the project grows
    }

    public static void Save()
    {
        var data = new SaveData
        {
            shopFunds   = ShopManager.Instance.shopFunds,
            currentDay  = DayCycleManager.Instance.CurrentDay,
            currentHour = DayCycleManager.Instance.CurrentHour,
            shopLevel   = ShopUpgradeSystem.Instance.ShopLevel,
        };

        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[Save] Game saved to {SavePath}");
    }

    public static SaveData Load()
    {
        if (!File.Exists(SavePath)) return null;
        string json = File.ReadAllText(SavePath);
        var data = JsonUtility.FromJson<SaveData>(json);
        Debug.Log("[Save] Game loaded.");
        return data;
    }

    public static bool SaveExists() => File.Exists(SavePath);
    public static void DeleteSave() => File.Delete(SavePath);
}
