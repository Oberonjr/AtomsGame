using UnityEngine;
using System.Linq;

public class CombatManager : MonoBehaviour
{
    private static CombatManager _instance;
    public static CombatManager Instance => _instance;
    
    public float TargetUpdateInterval = 1f;
    private float _updateTimer;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            Debug.Log("[CombatManager] Instance created");
        }
        else
        {
            Debug.LogWarning("[CombatManager] Duplicate instance destroyed");
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    void Update()
    {
        if (GameStateManager.Instance == null) return;
        if (GameStateManager.Instance.CurrentState != GameState.Simulate) return;
        if (GameStateManager.Instance.IsPaused) return;

        _updateTimer += Time.deltaTime;
        if (_updateTimer >= TargetUpdateInterval)
        {
            _updateTimer = 0f;
            UpdateAllTargets();
        }
    }

    /// <summary>
    /// Force immediate target assignment (called when simulation starts)
    /// </summary>
    public void AssignInitialTargets()
    {
        Debug.Log("[CombatManager] Assigning initial targets");
        _updateTimer = 0f;
        UpdateAllTargets();
    }

    private void UpdateAllTargets()
    {
        if (TeamManager.Instance == null) return;

        int targetsAssigned = 0;

        foreach (Team team in TeamManager.Instance.Teams)
        {
            if (team == null) continue;

            foreach (var kvp in team.Units)
            {
                if (kvp.Value == null) continue;

                // CHANGED: Use ToList() to avoid modification during iteration
                foreach (Troop troop in kvp.Value.ToList())
                {
                    // ROBUST NULL AND DEAD CHECKS
                    if (troop == null || troop.IsDead || troop.gameObject == null)
                    {
                        continue; // Skip dead/destroyed troops
                    }

                    // Only assign if no target or target is dead
                    if (troop.Target == null || troop.Target.IsDead || troop.Target.gameObject == null)
                    {
                        Troop nearestEnemy = FindNearestEnemy(troop, team);
                        if (nearestEnemy != null)
                        {
                            troop.SetTarget(nearestEnemy);
                            targetsAssigned++;
                        }
                    }
                }
            }
        }

        if (targetsAssigned > 0)
        {
            Debug.Log($"[CombatManager] Assigned {targetsAssigned} targets");
        }
    }

    private Troop FindNearestEnemy(Troop troop, Team friendlyTeam)
    {
        if (troop == null || friendlyTeam == null || troop.IsDead) return null;

        Troop nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Team enemyTeam in TeamManager.Instance.Teams)
        {
            if (enemyTeam == null || enemyTeam.TeamIndex == friendlyTeam.TeamIndex) continue;

            foreach (var kvp in enemyTeam.Units)
            {
                if (kvp.Value == null) continue;

                // CHANGED: Use ToList() to avoid modification during iteration
                foreach (Troop enemy in kvp.Value.ToList())
                {
                    // ROBUST NULL AND DEAD CHECKS
                    if (enemy == null || enemy.IsDead || enemy.gameObject == null) continue;

                    float distance = Vector3.Distance(troop.transform.position, enemy.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = enemy;
                    }
                }
            }
        }

        return nearest;
    }
}