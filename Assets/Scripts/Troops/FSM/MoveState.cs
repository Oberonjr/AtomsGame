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
        if (_troop.Target != null && _agent != null && _agent.isOnNavMesh)
        {
            Vector3 destination = _troop.Target.transform.position;
            destination.z = 0f; // Lock Z for 2D
            _agent.SetDestination(destination);
        }
    }

    public void Update()
    {
        if (_troop.Target == null || _troop.Target.CurrentHealth <= 0)
        {
            _troop.SetTarget(null);
            return;
        }

        if (_agent != null && _agent.isOnNavMesh)
        {
            Vector3 destination = _troop.Target.transform.position;
            destination.z = 0f; // Lock Z for 2D
            _agent.SetDestination(destination);
        }

        if (_troop.IsInRange(_troop.Target))
        {
            _troop.SetTarget(_troop.Target); // Switch to attack
        }
    }

    public void Exit()
    {
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.ResetPath();
        }
    }
}