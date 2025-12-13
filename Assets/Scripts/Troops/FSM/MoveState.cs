using UnityEngine;

public class MoveState : State
{
    private Troop _troop;
    private UnityEngine.AI.NavMeshAgent _agent;
    private Vector3 _lastDestination;
    private float _stuckCheckTimer = 0f;
    private float _stuckCheckInterval = 0.5f;
    private Vector3 _lastPosition;
    private float _minMovementThreshold = 0.1f;

    public MoveState(Troop troop)
    {
        _troop = troop;
        _agent = _troop.Agent;
        _lastDestination = Vector3.zero;
        _lastPosition = troop.transform.position;
    }

    public void Enter()
    {
        if (_troop.Target != null && _agent != null && _agent.isOnNavMesh && _agent.enabled)
        {
            Vector3 destination = _troop.Target.transform.position;
            destination.z = 0f;
            _agent.SetDestination(destination);
            _agent.isStopped = false; // Ensure movement is enabled
            
            _lastDestination = destination;
            _lastPosition = _troop.transform.position;
            
            // ? SECTION 12: Track initial path calculation
            if (PerformanceProfiler.Instance != null && _agent.hasPath)
            {
                PerformanceProfiler.Instance.RecordNavMeshRecalculation();
                
                // Track path length
                float pathLength = CalculatePathLength(_agent.path);
                PerformanceProfiler.Instance.RecordNavMeshPathLength(pathLength);
            }
        }
    }

    public void Update()
    {
        // ROBUST NULL AND DEAD CHECKS
        if (_troop.Target == null || _troop.Target.IsDead || _troop.Target.gameObject == null)
        {
            _troop.SetTarget(null);
            return;
        }

        // Update destination continuously
        if (_agent != null && _agent.isOnNavMesh && _agent.enabled)
        {
            Vector3 destination = _troop.Target.transform.position;
            destination.z = 0f;
            
            // ? SECTION 12: Track path recalculation (when destination changes significantly)
            if (Vector3.Distance(destination, _lastDestination) > 1f)
            {
                _agent.SetDestination(destination);
                _lastDestination = destination;
                
                if (PerformanceProfiler.Instance != null && _agent.hasPath)
                {
                    PerformanceProfiler.Instance.RecordNavMeshRecalculation();
                    
                    // Track new path length
                    float pathLength = CalculatePathLength(_agent.path);
                    PerformanceProfiler.Instance.RecordNavMeshPathLength(pathLength);
                }
            }
            
            // ? SECTION 12: Check if agent is stuck
            _stuckCheckTimer += Time.deltaTime;
            if (_stuckCheckTimer >= _stuckCheckInterval)
            {
                _stuckCheckTimer = 0f;
                
                float distanceMoved = Vector3.Distance(_troop.transform.position, _lastPosition);
                
                // If agent hasn't moved much but has a path and isn't stopped
                if (distanceMoved < _minMovementThreshold && _agent.hasPath && !_agent.isStopped && _agent.remainingDistance > _agent.stoppingDistance)
                {
                    if (PerformanceProfiler.Instance != null)
                    {
                        PerformanceProfiler.Instance.RecordNavMeshStuck();
                    }
                }
                
                _lastPosition = _troop.transform.position;
            }
        }

        // Check if we're in range to attack
        if (_troop.IsInRange(_troop.Target))
        {
            _troop.SetTarget(_troop.Target); // Switch to attack state
        }
    }

    public void Exit()
    {
        if (_agent != null && _agent.isOnNavMesh && _agent.enabled)
        {
            _agent.ResetPath();
        }
    }
    
    // ? SECTION 12: Calculate total path length
    private float CalculatePathLength(UnityEngine.AI.NavMeshPath path)
    {
        if (path == null || path.corners.Length < 2)
            return 0f;
        
        float length = 0f;
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            length += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }
        
        return length;
    }
}