using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityAtoms;

public class SaveLoadManager : MonoBehaviour
{
    private const string SAVE_DIRECTORY = "BattleLayouts";

    private List<TroopSnapshot> _prepStateSnapshot = new List<TroopSnapshot>();

    // ========== LAYOUT SAVE/LOAD (Cross-Compatible) ==========

    public void SaveLayout(string layoutName, List<ITroop> troops)
    {
        BattleLayout layout = new BattleLayout
        {
            LayoutName = layoutName,
            DateCreated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        foreach (ITroop troop in troops)
        {
            if (troop == null) continue;

            SavedTroop savedTroop = new SavedTroop
            {
                // Save TroopType enum (works for both Unity and Atoms)
                TroopTypeName = GetTroopType(troop).ToString(),
                Position = troop.Transform.position,
                Rotation = troop.Transform.rotation,
                TeamIndex = GetTeamIndex(troop),
                CurrentHealth = troop.CurrentHealth
            };

            layout.SavedTroops.Add(savedTroop);
        }

        // Save to file
        string json = JsonUtility.ToJson(layout, true);
        string directoryPath = Path.Combine(Application.persistentDataPath, "Layouts");
        
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        
        string filePath = Path.Combine(directoryPath, $"{layoutName}.json");
        File.WriteAllText(filePath, json);
        
        Debug.Log($"[SaveLoadManager] Saved layout '{layoutName}' with {layout.SavedTroops.Count} troops to {filePath}");
    }

    public void LoadLayout(string filePath, List<ITroop> activeTroops, List<UnitSelectionData> availableUnits, UnitSpawner spawner)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"[SaveLoadManager] Layout file not found: {filePath}");
            return;
        }

        string json = File.ReadAllText(filePath);
        BattleLayout layout = JsonUtility.FromJson<BattleLayout>(json);

        if (layout == null)
        {
            Debug.LogError("[SaveLoadManager] Failed to parse layout file");
            return;
        }

        Debug.Log($"[SaveLoadManager] Loading layout '{layout.LayoutName}' with {layout.SavedTroops.Count} troops");

        activeTroops.Clear();

        // Determine which mode to use (from SimulationConfig)
        bool useAtoms = SimulationConfig.Instance != null && 
                        SimulationConfig.Instance.Mode == SimulationMode.Atoms;

        foreach (SavedTroop savedTroop in layout.SavedTroops)
        {
            // Parse TroopType from saved string
            if (!Enum.TryParse(savedTroop.TroopTypeName, out TroopType troopType))
            {
                Debug.LogWarning($"[SaveLoadManager] Unknown TroopType: {savedTroop.TroopTypeName}");
                continue;
            }

            // Find matching unit data based on TroopType
            UnitSelectionData unitData = availableUnits.Find(u =>
            {
                if (u == null) return false;
                
                if (useAtoms && u.TroopPrefab_Atoms != null)
                {
                    // FIXED: Don't access .Stats property - it might not be initialized yet
                    // Instead, check the prefab's serialized field directly
                    var statsInstancer = u.TroopPrefab_Atoms.GetComponentInChildren<TroopStats_AtomsVariableInstancer>();
                    if (statsInstancer != null && statsInstancer.Base != null)
                    {
                        // Access the base variable's value (the ScriptableObject asset)
                        return statsInstancer.Base.Value.TroopType == troopType;
                    }
                    return false;
                }
                else if (u.TroopPrefab != null && u.TroopPrefab.TroopStats != null)
                {
                    return u.TroopPrefab.TroopStats.TroopType == troopType;
                }
                return false;
            });

            if (unitData == null)
            {
                Debug.LogWarning($"[SaveLoadManager] Could not find unit data for: {troopType} in {(useAtoms ? "Atoms" : "Unity")} mode");
                continue;
            }

            // Spawn using current mode
            GameObject prefab = useAtoms ? unitData.TroopPrefab_Atoms?.gameObject : unitData.TroopPrefab?.gameObject;
            
            if (prefab == null)
            {
                Debug.LogWarning($"[SaveLoadManager] Prefab is null for {unitData.DisplayName} in {(useAtoms ? "Atoms" : "Unity")} mode");
                continue;
            }

            GameObject troopObj = Instantiate(prefab, savedTroop.Position, savedTroop.Rotation);
            ITroop troop = troopObj.GetComponent<ITroop>() as ITroop;

            if (troop != null)
            {
                // Set properties (works for both)
                SetTeamIndex(troop, savedTroop.TeamIndex);
                troop.CurrentHealth = savedTroop.CurrentHealth;

                // Register to team
                ITeam team = TeamManager.Instance?.GetTeamByIndex(savedTroop.TeamIndex);
                if (team != null)
                {
                    team.RegisterUnit(troop);
                    Debug.Log($"[SaveLoadManager] Loaded {troopType} for team {savedTroop.TeamIndex}");
                }

                activeTroops.Add(troop);
            }
        }

        Debug.Log($"[SaveLoadManager] Loaded {activeTroops.Count} troops from layout in {(useAtoms ? "Atoms" : "Unity")} mode");
    }

    public List<string> GetSavedLayouts()
    {
        string directoryPath = Path.Combine(Application.persistentDataPath, "Layouts");
        
        if (!Directory.Exists(directoryPath))
        {
            return new List<string>();
        }

        string[] files = Directory.GetFiles(directoryPath, "*.json");
        return new List<string>(files);
    }

    // ========== PREP STATE SNAPSHOT ==========

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
            if (troop != null && 
                troop.GameObject != null && 
                !troop.IsDead)
            {
                _prepStateSnapshot.Add(new TroopSnapshot(troop));
                savedCount++;
            }
        }
        
        Debug.Log($"[SaveLoadManager] Saved prep state with {savedCount} troops");
    }

    public void RestorePrepState(List<ITroop> activeTroops, List<UnitSelectionData> availableUnits, UnitSpawner spawner)
    {
        if (activeTroops == null || availableUnits == null || spawner == null)
        {
            Debug.LogError("[SaveLoadManager] Null parameters in RestorePrepState");
            return;
        }
        
        activeTroops.Clear();

        bool useAtoms = SimulationConfig.Instance != null && 
                        SimulationConfig.Instance.Mode == SimulationMode.Atoms;

        foreach (TroopSnapshot snapshot in _prepStateSnapshot)
        {
            if (snapshot == null) continue;
            
            UnitSelectionData unitData = availableUnits.Find(u =>
            {
                if (u == null) return false;
                
                if (useAtoms && u.TroopPrefab_Atoms != null)
                {
                    var statsInstancer = u.TroopPrefab_Atoms.GetComponent<TroopStats_AtomsVariableInstancer>();
                    if (statsInstancer != null)
                    {
                        // ADDED: Try Base first, fallback to Variable if Base is null
                        if (statsInstancer.Base != null && statsInstancer.Base.Value.TroopType != TroopType.MELEE)
                        {
                            return statsInstancer.Base.Value.TroopType == snapshot.TroopType;
                        }
                        else if (statsInstancer.Variable != null && statsInstancer.Variable.Value.TroopType != TroopType.MELEE)
                        {
                            return statsInstancer.Variable.Value.TroopType == snapshot.TroopType;
                        }
                        else
                        {
                            // ADDED: Last resort - check prefab name
                            string prefabName = u.TroopPrefab_Atoms.name.ToLower();
                            return (snapshot.TroopType == TroopType.MELEE && prefabName.Contains("close")) ||
                                   (snapshot.TroopType == TroopType.RANGED && prefabName.Contains("far")) ||
                                   (snapshot.TroopType == TroopType.ARTILLERY && prefabName.Contains("artillery"));
                        }
                    }
                    return false;
                }
                else if (u.TroopPrefab != null && u.TroopPrefab.TroopStats != null)
                {
                    return u.TroopPrefab.TroopStats.TroopType == snapshot.TroopType;
                }
                return false;
            });

            if (unitData == null)
            {
                Debug.LogWarning($"[SaveLoadManager] Could not find unit data for TroopType: {snapshot.TroopType} in {(useAtoms ? "Atoms" : "Unity")} mode");
                
                // ADDED: Log available prefabs for debugging
                Debug.LogWarning($"[SaveLoadManager] Available Atoms prefabs:");
                foreach (var u in availableUnits)
                {
                    if (u != null && u.TroopPrefab_Atoms != null)
                    {
                        var statsInstancer = u.TroopPrefab_Atoms.GetComponent<TroopStats_AtomsVariableInstancer>();
                        if (statsInstancer != null && statsInstancer.Base != null)
                        {
                            Debug.LogWarning($"  - {u.DisplayName}: {statsInstancer.Base.Value.TroopType}");
                        }
                        else
                        {
                            Debug.LogWarning($"  - {u.DisplayName}: NO BASE ASSIGNED");
                        }
                    }
                }
                continue;
            }

            GameObject prefab = useAtoms ? unitData.TroopPrefab_Atoms?.gameObject : unitData.TroopPrefab?.gameObject;
            if (prefab == null)
            {
                Debug.LogWarning($"[SaveLoadManager] Prefab is null for {unitData.DisplayName}");
                continue;
            }

            GameObject troopObj = Instantiate(prefab, snapshot.Position, snapshot.Rotation);
            ITroop troop = troopObj.GetComponent<ITroop>() as ITroop;

            if (troop != null)
            {
                SetTeamIndex(troop, snapshot.TeamIndex);
                
                ITeam team = TeamManager.Instance?.GetTeamByIndex(snapshot.TeamIndex);
                if (team != null)
                {
                    team.RegisterUnit(troop);
                }

                activeTroops.Add(troop);
            }
        }

        Debug.Log($"[SaveLoadManager] Restored {activeTroops.Count} troops in {(useAtoms ? "Atoms" : "Unity")} mode");
    }

    // ========== HELPER METHODS ==========

    private TroopType GetTroopType(ITroop troop)
    {
        if (troop is Troop unityTroop && unityTroop.TroopStats != null)
        {
            return unityTroop.TroopStats.TroopType;
        }
        else if (troop is Troop_Atoms atomsTroop)
        {
            return atomsTroop.Stats.TroopType;
        }

        return TroopType.MELEE; // Fallback
    }

    private int GetTeamIndex(ITroop troop)
    {
        if (troop is Troop unityTroop)
        {
            return unityTroop.TeamIndex;
        }
        else if (troop is Troop_Atoms atomsTroop)
        {
            return atomsTroop.TeamIndex;
        }

        return 0; // Fallback
    }

    private void SetTeamIndex(ITroop troop, int teamIndex)
    {
        if (troop is Troop unityTroop)
        {
            unityTroop.TeamIndex = teamIndex;
        }
        else if (troop is Troop_Atoms atomsTroop)
        {
            atomsTroop.TeamIndex = teamIndex;
        }
    }
}