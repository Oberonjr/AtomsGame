using UnityEngine;

public class MoveState : State
{
    private Troop _troop;
    private UnityEngine.AI.NavMeshAgent _agent;

    public MoveState(Troop troop)
    {
        _troop = troop;
        _agent = _troop.Agent;
    }

    public void Enter()
    {
        if (_troop.Target != null && _agent != null && _agent.isOnNavMesh && _agent.enabled)
        {
            Vector3 destination = _troop.Target.transform.position;
            destination.z = 0f;
            _agent.SetDestination(destination);
            _agent.isStopped = false; // Ensure movement is enabled
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
            _agent.SetDestination(destination);
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
}