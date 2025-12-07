using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

/// <summary>
/// Handles saving and loading battle layouts
/// </summary>
public class SaveLoadManager : MonoBehaviour
{
    private const string SAVE_DIRECTORY = "BattleLayouts";

    private List<TroopSnapshot> _prepStateSnapshot = new List<TroopSnapshot>();

    public void SavePrepState(List<ITroop> troops)
    {
        _prepStateSnapshot.Clear();
        
        if (troops == null)
        {
            Debug.LogWarning("[SaveLoadManager] Troops list is null");
            return;
        }
        
        int savedCount = 0;
        foreach (ITroop troop in troops)
        {
            // ROBUST NULL CHECKS
            if (troop != null && 
                troop.GameObject != null && 
                troop is Troop troopMono && 
                !troopMono.IsDead)
            {
                _prepStateSnapshot.Add(new TroopSnapshot(troopMono));
                savedCount++;
            }
        }
        
        Debug.Log($"[SaveLoadManager] Saved {savedCount} troops (from {troops.Count} total)");
    }

    public void RestorePrepState(List<ITroop> activeTroops, List<UnitSelectionData> availableUnits, UnitSpawner spawner)
    {
        if (activeTroops == null || availableUnits == null || spawner == null) // NULL CHECK
        {
            Debug.LogError("[SaveLoadManager] Null parameters in RestorePrepState");
            return;
        }
        
        activeTroops.Clear();

        foreach (TroopSnapshot snapshot in _prepStateSnapshot)
        {
            if (snapshot == null || snapshot.Stats == null) continue; // NULL CHECK
            
            UnitSelectionData unitData = availableUnits.Find(u =>
                u?.TroopPrefab != null &&
                u.TroopPrefab.TroopStats != null &&
                u.TroopPrefab.TroopStats == snapshot.Stats);

            if (unitData?.TroopPrefab != null)
            {
                Troop troop = Instantiate(unitData.TroopPrefab, snapshot.Position, snapshot.Rotation);

                if (troop != null)
                {
                    troop.TeamIndex = snapshot.TeamIndex; // CHANGED: Use TeamIndex
                    troop.CurrentHealth = snapshot.CurrentHealth;
                    troop.IsAIActive = false;

                    ITeam team = TeamManager.Instance?.GetTeamByIndex(snapshot.TeamIndex); // CHANGED
                    if (team != null)
                    {
                        team.RegisterUnit(troop);
                    }

                    activeTroops.Add(troop);
                }
            }
        }

        Debug.Log($"[SaveLoadManager] Restored {activeTroops.Count} troops");
    }

    public void SaveLayout(string layoutName, List<ITroop> troops)
    {
        BattleLayout layout = new BattleLayout
        {
            LayoutName = layoutName,
            DateCreated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        foreach (ITroop troop in troops)
        {
            if (troop == null || troop.TroopStats == null) continue;

            Troop troopMono = troop as Troop;
            if (troopMono == null) continue;

            SavedTroop savedTroop = new SavedTroop
            {
                TroopStatsName = troop.TroopStats.name,
                Position = troop.Transform.position,
                Rotation = troop.Transform.rotation,
                TeamIndex = troopMono.TeamIndex, // CHANGED: Save index instead of GUID
                CurrentHealth = troop.CurrentHealth
            };

            layout.SavedTroops.Add(savedTroop);
        }

        // Create save directory
        string saveDir = Path.Combine(Application.persistentDataPath, SAVE_DIRECTORY);
        if (!Directory.Exists(saveDir))
        {
            Directory.CreateDirectory(saveDir);
        }

        // Save to JSON
        string fileName = $"{layoutName}.json";
        string filePath = Path.Combine(saveDir, fileName);

        if (File.Exists(filePath))
        {
            fileName = $"{layoutName}_{DateTime.Now:yyMMdd_HHmmss}.json";
            filePath = Path.Combine(saveDir, fileName);
        }

        string json = JsonUtility.ToJson(layout, true);
        File.WriteAllText(filePath, json);

        Debug.Log($"[SaveLoadManager] Saved layout '{layoutName}' with {layout.SavedTroops.Count} troops to: {filePath}");
    }

    public void LoadLayout(string filePath, List<ITroop> activeTroops, List<UnitSelectionData> availableUnits, UnitSpawner spawner)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"[SaveLoadManager] Layout file not found: {filePath}");
            return;
        }

        // Load JSON
        string json = File.ReadAllText(filePath);
        BattleLayout layout = JsonUtility.FromJson<BattleLayout>(json);

        if (layout == null)
        {
            Debug.LogError("[SaveLoadManager] Failed to parse layout file");
            return;
        }

        Debug.Log($"[SaveLoadManager] Loading layout '{layout.LayoutName}' with {layout.SavedTroops.Count} troops");

        activeTroops.Clear();

        // Spawn troops
        foreach (SavedTroop savedTroop in layout.SavedTroops)
        {
            UnitSelectionData unitData = availableUnits.Find(u =>
                u?.TroopPrefab != null &&
                u.TroopPrefab.TroopStats != null &&
                u.TroopPrefab.TroopStats.name == savedTroop.TroopStatsName);

            if (unitData == null)
            {
                Debug.LogWarning($"[SaveLoadManager] Could not find unit data for: {savedTroop.TroopStatsName}");
                continue;
            }

            Troop troop = Instantiate(unitData.TroopPrefab, savedTroop.Position, savedTroop.Rotation);

            if (troop != null)
            {
                troop.TeamIndex = savedTroop.TeamIndex; // CHANGED: Load index
                troop.CurrentHealth = savedTroop.CurrentHealth;
                troop.IsAIActive = false;
                
                Vector3 pos = troop.transform.position;
                pos.z = 0f;
                troop.transform.position = pos;

                // Find team by index
                ITeam team = TeamManager.Instance?.GetTeamByIndex(savedTroop.TeamIndex);
                if (team != null)
                {
                    team.RegisterUnit(troop);
                    Debug.Log($"[SaveLoadManager] Registered {troop.name} to team {savedTroop.TeamIndex}");
                }

                activeTroops.Add(troop);
            }
        }

        Debug.Log($"[SaveLoadManager] Loaded {activeTroops.Count} troops from layout '{layout.LayoutName}'");
        
        // FIX 4: Log team verification
        if (TeamManager.Instance != null)
        {
            foreach (Team team in TeamManager.Instance.Teams)
            {
                if (team != null)
                {
                    Debug.Log($"[SaveLoadManager] Team {team.ID} has {team.TotalUnits()} units after load");
                }
            }
        }
    }

    public List<string> GetSavedLayouts()
    {
        string saveDir = Path.Combine(Application.persistentDataPath, SAVE_DIRECTORY);
        if (!Directory.Exists(saveDir))
        {
            return new List<string>();
        }

        string[] files = Directory.GetFiles(saveDir, "*.json");
        return new List<string>(files);
    }
}