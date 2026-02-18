using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Saves/loads game state to PlayerPrefs via JSON.
/// Uses string serialization for doubles (JsonUtility doesn't support double natively).
/// </summary>
public static class SaveSystem
{
    private const string SaveKey = "CellGameSave";

    [Serializable]
    public class SaveData
    {
        // Stored as strings so JsonUtility round-trips doubles without precision loss
        public string cellCountStr       = "0";
        public string cellsPerClickStr   = "1";
        public string cellsPerSecondStr  = "0";
        public string cpcMultiplierStr   = "1";

        public List<string> unlockedNodeIds     = new List<string>();
        public List<int>    shopPurchaseCounts  = new List<int>();
    }

    public static void Save(SaveData data)
    {
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
        Debug.Log("[SaveSystem] Saved.");
    }

    /// Returns null if no save exists.
    public static SaveData Load()
    {
        if (!PlayerPrefs.HasKey(SaveKey)) return null;
        string json = PlayerPrefs.GetString(SaveKey);
        try
        {
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] Failed to parse save: {e.Message}");
            return null;
        }
    }

    public static void DeleteAll()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        Debug.Log("[SaveSystem] Save deleted.");
    }
}
