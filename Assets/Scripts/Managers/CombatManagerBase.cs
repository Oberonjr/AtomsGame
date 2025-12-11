using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Base class for combat management - contains all shared logic
/// Subclasses: CombatManager (Unity), CombatManager_Atoms (Atoms)
/// </summary>
public abstract class CombatManagerBase : MonoBehaviour, ICombatManager
{
    [Header("Settings")]
    [SerializeField] protected float _targetUpdateInterval = 1f;

    [Header("Stuck Detection")]
    [SerializeField] protected float _stuckCheckInterval = 5f;
    [SerializeField] protected float _stuckDistanceThreshold = 0.1f;

    [Header("Debug Visualization")]
    [SerializeField] protected bool _showStuckDebugGizmos = false;

    protected float _updateTimer;
    protected bool _isInitialized;

    // Stuck detection tracking
    protected Dictionary<ITroop, Vector3> _lastKnownPositions = new Dictionary<ITroop, Vector3>();
    protected Dictionary<ITroop, float> _stuckTimers = new Dictionary<ITroop, float>();
    protected float _globalStuckCheckTimer = 0f;

    // ========== UNITY LIFECYCLE ==========

    protected virtual void Update()
    {
        if (!_isInitialized) return;
        if (GameStateManager.Instance == null) return;
        if (GameStateManager.Instance.CurrentState != GameState.Simulate) return;
        if (GameStateManager.Instance.IsPaused) return;

        // Target update
        _updateTimer += Time.deltaTime;
        if (_updateTimer >= _targetUpdateInterval)
        {
            _updateTimer = 0f;
            UpdateCombat();
        }

        // Stuck detection
        _globalStuckCheckTimer += Time.deltaTime;
        if (_globalStuckCheckTimer >= _stuckCheckInterval)
        {
            _globalStuckCheckTimer = 0f;
            CheckForStuckTroops();
        }
    }

    void OnDrawGizmos()
    {
        if (!_showStuckDebugGizmos || !Application.isPlaying) return;

        foreach (var kvp in _stuckTimers)
        {
            ITroop troop = kvp.Key;
            float stuckTime = kvp.Value;

            if (troop == null || troop.GameObject == null) continue;

            // Draw red sphere above stuck troops
            Gizmos.color = Color.Lerp(Color.yellow, Color.red, stuckTime / (_stuckCheckInterval * 2));
            Gizmos.DrawWireSphere(troop.Transform.position + Vector3.up * 2, 0.5f);

            // Draw line to target if exists
            if (troop.Target != null && troop.Target.GameObject != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(troop.Transform.position, troop.Target.Transform.position);
            }
        }
    }

    // ========== ICombatManager IMPLEMENTATION ==========

    public virtual void Initialize()
    {
        Debug.Log($"[{GetType().Name}] Initializing");

        _isInitialized = true;
        _updateTimer = 0f;

        // Clear stuck detection data
        _lastKnownPositions.Clear();
        _stuckTimers.Clear();
        _globalStuckCheckTimer = 0f;

        // Hook for subclasses
        OnInitialize();

        Debug.Log($"[{GetType().Name}] Initialized successfully");
    }

    public virtual void Cleanup()
    {
        Debug.Log($"[{GetType().Name}] Cleaning up");

        _isInitialized = false;
        _updateTimer = 0f;

        // Clear stuck detection data
        _lastKnownPositions.Clear();
        _stuckTimers.Clear();

        // Hook for subclasses
        OnCleanup();
    }

    public virtual void UpdateCombat()
    {
        if (TeamManager.Instance == null) return;
        
        int targetsAssigned = 0;
        
        // Iterate through all troops via TeamManager
        foreach (Team team in TeamManager.Instance.Teams)
        {
            if (team == null) continue;
            
            // ADDED: Check if Units dictionary is null
            if (team.Units == null)
            {
                Debug.LogWarning($"[{GetType().Name}] Team {team.TeamIndex} has null Units dictionary");
                continue;
            }
            
            foreach (var kvp in team.Units)
            {
                if (kvp.Value == null) continue;
                
                foreach (ITroop troop in kvp.Value)
                {
                    // Skip invalid/dead troops
                    if (troop == null || troop.IsDead)
                        continue;
                    
                    // ADDED: Check GameObject before accessing
                    if (troop.GameObject == null)
                        continue;
                    
                    // Check if needs new target
                    if (troop.Target == null || troop.Target.IsDead || troop.Target.GameObject == null)
                    {
                        ITroop nearestEnemy = FindNearestEnemy(troop);
                        if (nearestEnemy != null)
                        {
                            troop.SetTarget(nearestEnemy);
                            targetsAssigned++;
                            
                            // Hook for subclasses (e.g., Atoms events)
                            OnTargetAssigned(troop, nearestEnemy);
                        }
                    }
                }
            }
        }
        
        if (targetsAssigned > 0)
        {
            Debug.Log($"[{GetType().Name}] Assigned {targetsAssigned} targets");
        }
    }

    public virtual ITroop FindNearestEnemy(ITroop sourceTroop)
    {
        if (sourceTroop == null || TeamManager.Instance == null) return null;

        int sourceTeamIndex = GetTeamIndex(sourceTroop);
        if (sourceTeamIndex < 0) return null;

        ITroop nearestEnemy = null;
        float nearestDistance = float.MaxValue;

        foreach (Team enemyTeam in TeamManager.Instance.Teams)
        {
            if (enemyTeam == null || enemyTeam.TeamIndex == sourceTeamIndex) continue;

            foreach (var kvp in enemyTeam.Units)
            {
                if (kvp.Value == null) continue;

                foreach (ITroop candidate in kvp.Value)
                {
                    if (candidate == null || candidate.IsDead || candidate.GameObject == null)
                        continue;

                    float distance = Vector3.Distance(
                        sourceTroop.Transform.position,
                        candidate.Transform.position
                    );

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = candidate;
                    }
                }
            }
        }

        return nearestEnemy;
    }

    public virtual void RegisterTroop(ITroop troop)
    {
        // No-op - troops are tracked via TeamManager
        Debug.Log($"[{GetType().Name}] RegisterTroop called (tracked via TeamManager)");
    }

    public virtual void UnregisterTroop(ITroop troop)
    {
        // No-op - troops are tracked via TeamManager
        Debug.Log($"[{GetType().Name}] UnregisterTroop called (tracked via TeamManager)");
    }

    // ========== STUCK DETECTION ==========

    protected void CheckForStuckTroops()
    {
        if (TeamManager.Instance == null) return;

        int unstuckCount = 0;

        foreach (Team team in TeamManager.Instance.Teams)
        {
            if (team == null) continue;

            foreach (var kvp in team.Units)
            {
                if (kvp.Value == null) continue;

                foreach (ITroop troop in kvp.Value)
                {
                    if (troop == null || troop.IsDead || troop.GameObject == null)
                        continue;

                    Vector3 currentPos = troop.Transform.position;

                    if (_lastKnownPositions.TryGetValue(troop, out Vector3 lastPos))
                    {
                        float distanceMoved = Vector3.Distance(currentPos, lastPos);

                        // CHANGED: Check if troop is actively engaging target
                        bool isEngaged = IsEngagedWithTarget(troop);

                        // Only consider stuck if:
                        // 1. Hasn't moved
                        // 2. Has a target
                        // 3. Is NOT engaged (not in range and attacking)
                        if (distanceMoved < _stuckDistanceThreshold && troop.Target != null && !isEngaged)
                        {
                            if (!_stuckTimers.ContainsKey(troop))
                            {
                                _stuckTimers[troop] = 0f;
                            }

                            _stuckTimers[troop] += _stuckCheckInterval;

                            // If stuck for 2 check intervals (10 seconds default), force reset
                            if (_stuckTimers[troop] >= _stuckCheckInterval * 2)
                            {
                                Debug.LogWarning($"[{GetType().Name}] {troop.GameObject.name} appears stuck! " +
                                               $"Target: {troop.Target?.GameObject?.name ?? "null"}, " +
                                               $"Distance moved: {distanceMoved:F3}, " +
                                               $"Engaged: {isEngaged}");
                                UnstuckTroop(troop);
                                unstuckCount++;
                            }
                        }
                        else
                        {
                            // Troop is moving OR engaged, clear stuck timer
                            _stuckTimers.Remove(troop);
                        }
                    }

                    _lastKnownPositions[troop] = currentPos;
                }
            }
        }

        if (unstuckCount > 0)
        {
            Debug.Log($"[{GetType().Name}] Unstuck {unstuckCount} frozen troops");
        }

        CleanupStuckDetectionDictionaries();
    }

    protected void UnstuckTroop(ITroop troop)
    {
        if (troop == null || troop.IsDead) return;

        string oldTargetName = troop.Target?.GameObject?.name ?? "null";

        // Clear current target
        troop.SetTarget(null);

        // Wait a frame for FSM to reset to idle
        // Then find a new target
        ITroop newTarget = FindNearestEnemy(troop);

        if (newTarget != null)
        {
            troop.SetTarget(newTarget);
            Debug.Log($"[{GetType().Name}] Unstuck {troop.GameObject.name}: " +
                     $"Old target: {oldTargetName}, " +
                     $"New target: {newTarget.GameObject.name}");
        }
        else
        {
            Debug.LogWarning($"[{GetType().Name}] Could not find new target for {troop.GameObject.name}");
        }

        // Reset stuck timer
        _stuckTimers.Remove(troop);

        // OPTIONAL: If using NavMeshAgent, reset its path
        ResetNavigation(troop);
    }

    /// <summary>
    /// Reset navigation for stuck troop (works for both Unity and Atoms)
    /// </summary>
    private void ResetNavigation(ITroop troop)
    {
        UnityEngine.AI.NavMeshAgent agent = null;

        if (troop is Troop unityTroop)
        {
            agent = unityTroop.Agent;
        }
        else if (troop is Troop_Atoms atomsTroop)
        {
            agent = atomsTroop.Agent;
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            // Reset path to clear any stuck state
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.isStopped = false;

            Debug.Log($"[{GetType().Name}] Reset navigation for {troop.GameObject.name}");
        }
    }

    protected void CleanupStuckDetectionDictionaries()
    {
        List<ITroop> toRemove = new List<ITroop>();

        foreach (var kvp in _lastKnownPositions)
        {
            if (kvp.Key == null || kvp.Key.IsDead || kvp.Key.GameObject == null)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var troop in toRemove)
        {
            _lastKnownPositions.Remove(troop);
            _stuckTimers.Remove(troop);
        }
    }

    // ========== UTILITY METHODS ==========

    public void AssignInitialTargets()
    {
        Debug.Log($"[{GetType().Name}] Assigning initial targets");
        _updateTimer = 0f;
        UpdateCombat();
    }

    public int GetActiveTroopCount()
    {
        if (TeamManager.Instance == null) return 0;

        int count = 0;
        foreach (Team team in TeamManager.Instance.Teams)
        {
            if (team != null)
            {
                count += team.TotalUnits();
            }
        }
        return count;
    }

    protected int GetTeamIndex(ITroop troop)
    {
        if (troop is Troop unityTroop)
        {
            return unityTroop.TeamIndex;
        }
        else if (troop is Troop_Atoms atomsTroop)
        {
            return atomsTroop.TeamIndex;
        }

        return -1;
    }

    // ========== ABSTRACT/VIRTUAL HOOKS FOR SUBCLASSES ==========

    /// <summary>
    /// Called after base Initialize() completes - override to add custom behavior
    /// </summary>
    protected virtual void OnInitialize() { }

    /// <summary>
    /// Called after base Cleanup() completes - override to add custom behavior
    /// </summary>
    protected virtual void OnCleanup() { }

    /// <summary>
    /// Called when a target is assigned - override to raise events, etc.
    /// </summary>
    protected virtual void OnTargetAssigned(ITroop troop, ITroop target) { }

    /// <summary>
    /// Debug method to log all troops and their states
    /// Call this if you're seeing persistent stuck issues
    /// </summary>
    [ContextMenu("Debug Log All Troop States")]
    public void DebugLogAllTroopStates()
    {
        if (TeamManager.Instance == null)
        {
            Debug.LogWarning("[CombatManagerBase] TeamManager is null");
            return;
        }

        Debug.Log("========== TROOP STATE DEBUG ==========");

        foreach (Team team in TeamManager.Instance.Teams)
        {
            if (team == null) continue;

            Debug.Log($"Team {team.TeamIndex}: {team.TotalUnits()} units");

            foreach (var kvp in team.Units)
            {
                if (kvp.Value == null) continue;

                foreach (ITroop troop in kvp.Value)
                {
                    if (troop == null || troop.GameObject == null) continue;

                    string targetInfo = "None";
                    float targetDistance = 0f;
                    bool inRange = false;

                    if (troop.Target != null && troop.Target.GameObject != null)
                    {
                        targetInfo = troop.Target.GameObject.name;
                        targetDistance = Vector3.Distance(troop.Transform.position, troop.Target.Transform.position);
                        inRange = targetDistance <= troop.GetAttackRange();
                    }

                    bool isStuck = _stuckTimers.ContainsKey(troop);
                    float stuckTime = isStuck ? _stuckTimers[troop] : 0f;

                    Debug.Log($"  {troop.GameObject.name} | " +
                             $"Dead: {troop.IsDead} | " +
                             $"Target: {targetInfo} | " +
                             $"Distance: {targetDistance:F2} | " +
                             $"In Range: {inRange} | " +
                             $"Stuck Timer: {stuckTime:F1}s");
                }
            }
        }

        Debug.Log("=======================================");
    }

    /// <summary>
    /// Check if troop is actively engaged with target (in attack range)
    /// Troops that are attacking are intentionally stationary
    /// </summary>
    private bool IsEngagedWithTarget(ITroop troop)
    {
        if (troop == null || troop.Target == null || troop.Target.IsDead || troop.Target.GameObject == null)
            return false;

        // Check if in attack range
        float attackRange = troop.GetAttackRange();
        float distance = Vector3.Distance(troop.Transform.position, troop.Target.Transform.position);

        // Consider engaged if within 110% of attack range (small buffer for floating point errors)
        return distance <= (attackRange * 1.1f);
    }
}