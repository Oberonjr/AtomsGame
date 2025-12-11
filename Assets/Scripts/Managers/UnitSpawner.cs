using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections.Generic;

/// <summary>
/// Handles unit spawning and removal (simulation-agnostic)
/// </summary>
public class UnitSpawner : MonoBehaviour
{
    private const float NAVMESH_SAMPLE_DISTANCE = 2f;

    public event Action<ITroop> OnTroopSpawned;
    public event Action<ITroop> OnTroopRemoved;

    /// <summary>
    /// Spawn a troop at a position in a team area
    /// </summary>
    public ITroop SpawnTroop(UnitSelectionData unitData, Vector3 position, TeamArea teamArea, ITeam team)
    {
        // Determine which prefab to use based on SimulationConfig
        SimulationMode mode = SimulationMode.Unity; // Default
        if (SimulationConfig.Instance != null)
        {
            mode = SimulationConfig.Instance.Mode;
        }
        
        GameObject prefabToSpawn = null;
        
        if (mode == SimulationMode.Atoms && unitData.TroopPrefab_Atoms != null)
        {
            prefabToSpawn = unitData.TroopPrefab_Atoms.gameObject;
        }
        else if (unitData.TroopPrefab != null)
        {
            prefabToSpawn = unitData.TroopPrefab.gameObject;
        }
        
        if (prefabToSpawn == null)
        {
            Debug.LogError($"[UnitSpawner] No prefab assigned for {unitData.DisplayName} in {mode} mode");
            return null;
        }
        
        Quaternion spawnRotation = GetSpawnRotation(teamArea);
        GameObject troopObj = Instantiate(prefabToSpawn, position, spawnRotation);
        
        if (troopObj == null)
        {
            Debug.LogError("[UnitSpawner] Failed to instantiate prefab");
            return null;
        }
        
        // Position correction
        Vector3 finalPos = troopObj.transform.position;
        finalPos.z = 0f;
        troopObj.transform.position = finalPos;
        troopObj.transform.rotation = spawnRotation;
        
        ITroop troop = troopObj.GetComponent<ITroop>() as ITroop;
        
        if (troop != null)
        {
            // Set team index based on type
            if (troop is Troop unityTroop)
            {
                unityTroop.TeamIndex = team.TeamIndex;
                Debug.Log($"[UnitSpawner] Spawned Unity {unitData.DisplayName} for team {team.TeamIndex}");
            }
            else if (troop is Troop_Atoms atomsTroop)
            {
                atomsTroop.TeamIndex = team.TeamIndex;
                Debug.Log($"[UnitSpawner] Spawned Atoms {unitData.DisplayName} for team {team.TeamIndex}");
            }
            
            team.RegisterUnit(troop);
            OnTroopSpawned?.Invoke(troop);
            
            return troop;
        }
        else
        {
            Debug.LogError($"[UnitSpawner] Spawned prefab has no ITroop component!");
            Destroy(troopObj);
            return null;
        }
    }

    /// <summary>
    /// Remove a troop from the game
    /// </summary>
    public void RemoveTroop(ITroop troop, ITeam team)
    {
        if (troop == null)
        {
            Debug.LogWarning("[UnitSpawner] Cannot remove null troop");
            return;
        }

        Debug.Log($"[UnitSpawner] Removing troop");

        // Notify team (null check)
        if (team != null)
        {
            team.OnUnitDied(troop);
        }
        else
        {
            Debug.LogWarning("[UnitSpawner] Team is null when removing troop");
        }

        // Notify listeners
        OnTroopRemoved?.Invoke(troop);

        // Destroy GameObject (null check)
        if (troop.GameObject != null)
        {
            Destroy(troop.GameObject);
        }
    }

    private Quaternion GetSpawnRotation(TeamArea spawnArea)
    {
        if (TeamManager.Instance == null || spawnArea == null)
        {
            return Quaternion.identity;
        }

        Vector3 enemyDirection = Vector3.zero;
        int enemyCount = 0;

        foreach (Team team in TeamManager.Instance.Teams)
        {
            if (team?.Area != null && team.Area != spawnArea)
            {
                Vector3 direction = (team.Area.transform.position - spawnArea.transform.position).normalized;
                enemyDirection += direction;
                enemyCount++;
            }
        }

        if (enemyCount > 0)
        {
            enemyDirection /= enemyCount;
            float angle = Mathf.Atan2(enemyDirection.y, enemyDirection.x) * Mathf.Rad2Deg;
            return Quaternion.Euler(0, 0, angle);
        }

        return Quaternion.identity;
    }
}