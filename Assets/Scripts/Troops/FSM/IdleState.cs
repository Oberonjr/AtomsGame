using UnityEngine;

public class IdleState : State
{
    private Troop _troop;

    public IdleState(Troop troop)
    {
        _troop = troop;
    }

    public void Enter()
    {
        // Stop movement when idle
        if (_troop.Agent != null && _troop.Agent.isOnNavMesh && _troop.Agent.enabled)
        {
            _troop.Agent.isStopped = true;
            _troop.Agent.velocity = Vector3.zero;
            _troop.Agent.ResetPath();
        }
    }

    public void Update()
    {
        // Stay idle - CombatManager will assign targets
        // No need to do anything here
    }

    public void Exit()
    {
        // Resume movement capability when leaving idle
        if (_troop != null && _troop.Agent != null && _troop.Agent.isOnNavMesh && _troop.Agent.enabled)
        {
            _troop.Agent.isStopped = false;
        }
    }
}   