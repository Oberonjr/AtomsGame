using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public float TargetUpdateInterval = 1f;
    private float _updateTimer;

    void Update()
    {
        // Only update during simulation and not paused
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

    private void UpdateAllTargets()
    {
        if (TeamManager.Instance == null) return;

        foreach (Team team in TeamManager.Instance.Teams)
        {
            foreach (var kvp in team.Units)
            {
                foreach (Troop troop in kvp.Value)
                {
                    if (troop == null) continue;

                    if (troop.Target == null || troop.Target.CurrentHealth <= 0)
                    {
                        Troop nearestEnemy = FindNearestEnemy(troop, team);
                        troop.SetTarget(nearestEnemy);
                    }
                }
            }
        }
    }

    private Troop FindNearestEnemy(Troop troop, Team friendlyTeam)
    {
        Troop nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Team enemyTeam in TeamManager.Instance.Teams)
        {
            if (enemyTeam.ID == friendlyTeam.ID) continue;

            foreach (var kvp in enemyTeam.Units)
            {
                foreach (Troop enemy in kvp.Value)
                {
                    if (enemy == null) continue;

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