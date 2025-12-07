using UnityEngine;

public class AttackState : State
{
    private Troop _troop;
    private TroopFSM _fsm;
    private float _attackTimer;
    private bool _isFirstAttack;

    public AttackState(Troop troop)
    {
        _troop = troop;
        _fsm = troop.FSM;
        _attackTimer = 0f;
        _isFirstAttack = true;
    }

    public void Enter()
    {
        // Use initial attack delay on first attack to prevent instant damage
        if (_troop.TroopStats != null)
        {
            _attackTimer = _troop.TroopStats.InitialAttackDelay;
        }
        else
        {
            _attackTimer = 0.3f; // Fallback default
        }
        
        _isFirstAttack = true;
        
        // Stop agent movement when entering attack state
        if (_troop.Agent != null && _troop.Agent.isOnNavMesh)
        {
            _troop.Agent.isStopped = true;
            _troop.Agent.velocity = Vector3.zero; // Ensure completely stopped
        }
    }

    public void Update()
    {
        // CHECK IF THIS TROOP IS DEAD
        if (_troop == null || _troop.IsDead)
        {
            return; // Stop updating immediately
        }
        
        // ROBUST NULL AND DEAD CHECKS FOR TARGET
        if (_troop.Target == null || _troop.Target.IsDead || _troop.Target.gameObject == null)
        {
            _troop.SetTarget(null); // Clear invalid target
            return;
        }

        // Check if target moved out of range
        if (!_troop.IsInRange(_troop.Target))
        {
            _troop.SetTarget(_troop.Target); // This will switch back to MoveState
            return;
        }

        // Attack timer countdown
        _attackTimer -= Time.deltaTime;
        
        if (_attackTimer <= 0f)
        {
            _troop.Attack();
            
            // After first attack, use normal attack cooldown
            if (_isFirstAttack)
            {
                _isFirstAttack = false;
            }
            
            _attackTimer = _troop.TroopStats.AttackCooldown;
        }
    }

    public void Exit()
    {
        // Resume agent movement when leaving attack state
        if (_troop != null && _troop.Agent != null && _troop.Agent.isOnNavMesh && _troop.Agent.enabled)
        {
            _troop.Agent.isStopped = false;
        }
    }
}