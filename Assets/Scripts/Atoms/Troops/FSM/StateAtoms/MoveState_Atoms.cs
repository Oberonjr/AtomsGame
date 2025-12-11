using UnityEngine;

public class MoveState_Atoms : IState_Atoms
{
    private Troop_Atoms _troop;
    private UnityEngine.AI.NavMeshAgent _agent;

    public MoveState_Atoms(Troop_Atoms troop)
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
            _agent.isStopped = false;
        }
    }

    public void Update()
    {
        // NULL AND DEAD CHECKS
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