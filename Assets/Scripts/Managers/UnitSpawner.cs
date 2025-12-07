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
        if (unitData?.TroopPrefab == null)
        {
            Debug.LogError("[UnitSpawner] Cannot spawn - unitData or TroopPrefab is null!");
            return null;
        }

        // Adjust position for 2D
        position.z = 0f;

        // Sample NavMesh
        if (NavMesh.SamplePosition(position, out NavMeshHit navHit, NAVMESH_SAMPLE_DISTANCE, NavMesh.AllAreas))
        {
            position = navHit.position;
            position.z = 0f;
        }

        // Calculate spawn rotation
        Quaternion spawnRotation = CalculateSpawnRotation(teamArea);

        // Instantiate troop
        Troop troop = Instantiate(unitData.TroopPrefab, position, spawnRotation);

        if (troop != null)
        {
            // Position correction
            Vector3 finalPos = troop.transform.position;
            finalPos.z = 0f;
            troop.transform.position = finalPos;
            troop.transform.rotation = spawnRotation;

            // Setup troop - use team index
            troop.IsAIActive = false;
            troop.TeamIndex = team.TeamIndex; // CHANGED: Use index instead of GUID

            // Register to team
            team.RegisterUnit(troop);

            // Notify listeners
            OnTroopSpawned?.Invoke(troop);

            Debug.Log($"[UnitSpawner] Spawned {unitData.DisplayName} for team {team.TeamIndex}");
            return troop;
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

    private Quaternion CalculateSpawnRotation(TeamArea spawnArea)
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