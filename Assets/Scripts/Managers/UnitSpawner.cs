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
        if (unitData == null || team == null)
        {
            Debug.LogError("[UnitSpawner] Cannot spawn - null data or team");
            return null;
        }

        // Determine which prefab to use based on mode
        bool useAtoms = SimulationConfig.Instance != null && 
                        SimulationConfig.Instance.Mode == SimulationMode.Atoms;

        GameObject prefab = useAtoms ? unitData.TroopPrefab_Atoms?.gameObject : unitData.TroopPrefab?.gameObject;

        if (prefab == null)
        {
            Debug.LogError($"[UnitSpawner] No prefab available for {unitData.DisplayName} in {(useAtoms ? "Atoms" : "Unity")} mode");
            return null;
        }

        ITroop troop = SpawnTroopInstance(prefab, position, team.TeamIndex);

        if (troop != null)
        {
            Debug.Log($"[UnitSpawner] Spawned {unitData.DisplayName} for team {team.TeamIndex} at {position}");

            // Register with team
            team.RegisterUnit(troop);
            
            Debug.Log($"[UnitSpawner] Team {team.TeamIndex} now has {team.TotalUnits()} units");

            OnTroopSpawned?.Invoke(troop);
        }
        else
        {
            Debug.LogError($"[UnitSpawner] Spawned object has no ITroop component: {prefab.name}");
        }

        return null;
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

        string troopName = troop.GameObject != null ? troop.GameObject.name : "Unknown";
        int teamIndex = team != null ? team.TeamIndex : -1;

        Debug.Log($"[UnitSpawner] Removing {troopName} from team {teamIndex}");

        // Unregister from team
        if (team != null)
        {
            team.OnUnitDied(troop);
            Debug.Log($"[UnitSpawner] Team {teamIndex} now has {team.TotalUnits()} units");
        }

        // Notify listeners
        OnTroopRemoved?.Invoke(troop);

        // Destroy GameObject
        if (troop.GameObject != null)
        {
            Destroy(troop.GameObject);
            Debug.Log($"[UnitSpawner] Destroyed {troopName}");
        }
    }

    private ITroop SpawnTroopInstance(GameObject prefab, Vector3 position, int teamIndex)
    {
        GameObject troopObj = Instantiate(prefab, position, Quaternion.identity);
        troopObj.name = $"{prefab.name}_{teamIndex}";

        ITroop troop = troopObj.GetComponent<ITroop>();
        if (troop != null)
        {
            troop.TeamIndex = teamIndex;
            
            // ADDED: Record spawn
            if (PerformanceProfiler.Instance != null)
            {
                PerformanceProfiler.Instance.RecordUnitSpawned();
            }
        }

        return troop;
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