using UnityEngine;

public class AttackState : State
{
    private Troop _troop;
    private float _attackTimer;

    public AttackState(Troop troop)
    {
        _troop = troop;
        _attackTimer = 0f;
    }

    public void Enter()
    {
        _attackTimer = 0f;
        
        // Stop agent movement when attacking
        if (_troop.Agent != null && _troop.Agent.isOnNavMesh)
        {
            _troop.Agent.isStopped = true;
        }
    }

    public void Update()
    {
        if (_troop.Target == null || _troop.Target.CurrentHealth <= 0)
        {
            _troop.SetTarget(null);
            return;
        }

        if (!_troop.IsInRange(_troop.Target))
        {
            _troop.SetTarget(_troop.Target);
            return;
        }

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= _troop.TroopStats.AttackCooldown)
        {
            _troop.Attack();
            // Removed reload trigger - handled by exit time in Animator
            _attackTimer = 0f;
        }
    }

    public void Exit()
    {
        // Resume agent movement
        if (_troop.Agent != null && _troop.Agent.isOnNavMesh)
        {
            _troop.Agent.isStopped = false;
        }
    }
}