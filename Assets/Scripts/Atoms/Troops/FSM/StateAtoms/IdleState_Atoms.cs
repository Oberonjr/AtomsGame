using UnityEngine;

public class IdleState_Atoms : IState_Atoms
{
    private Troop_Atoms _troop;

    public IdleState_Atoms(Troop_Atoms troop)
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