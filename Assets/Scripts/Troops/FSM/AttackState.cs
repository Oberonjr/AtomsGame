using UnityEngine;

public class AttackState : State
{
    private Troop _troop;
    private float _attackTimer;
    private bool _isFirstAttack;

    public AttackState(Troop troop)
    {
        _troop = troop;
        _attackTimer = 0f;
        _isFirstAttack = true;
    }

    public void Enter()
    {
        _attackTimer = 0f;
        _isFirstAttack = true; // Reset for new target
        
        // Stop agent movement when entering attack state
        if (_troop.Agent != null && _troop.Agent.isOnNavMesh)
        {
            _troop.Agent.isStopped = true;
        }
    }

    public void Update()
    {
        if (_troop.Target == null || _troop.Target.CurrentHealth <= 0 || _troop.Target.IsDead)
        {
            _troop.SetTarget(null);
            return;
        }

        if (!_troop.IsInRange(_troop.Target))
        {
            _troop.SetTarget(_troop.Target); // This will switch back to MoveState
            return;
        }

        _attackTimer += Time.deltaTime;
        
        // Use initial delay for first attack, then regular cooldown
        float requiredCooldown = _isFirstAttack 
            ? _troop.TroopStats.InitialAttackDelay 
            : _troop.TroopStats.AttackCooldown;
        
        if (_attackTimer >= requiredCooldown)
        {
            _troop.Attack();
            _attackTimer = 0f;
            _isFirstAttack = false; // Subsequent attacks use regular cooldown
        }
    }

    public void Exit()
    {
        // Resume agent movement when leaving attack state
        if (_troop.Agent != null && _troop.Agent.isOnNavMesh)
        {
            _troop.Agent.isStopped = false;
        }
    }
}