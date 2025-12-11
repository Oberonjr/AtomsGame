using UnityEngine;

public class AttackState_Atoms : IState_Atoms
{
    private Troop_Atoms _troop;
    private TroopFSM_Atoms _fsm;
    private float _attackTimer;
    private bool _isFirstAttack;

    public AttackState_Atoms(Troop_Atoms troop)
    {
        _troop = troop;
        _fsm = troop.FSM;
        _attackTimer = 0f;
        _isFirstAttack = true;
    }

    public void Enter()
    {
        // Use Troop_Atoms helper method - NO TroopStats reference
        _attackTimer = _troop.GetInitialAttackDelay();
        _isFirstAttack = true;

        // Stop agent movement
        if (_troop.Agent != null && _troop.Agent.isOnNavMesh)
        {
            _troop.Agent.isStopped = true;
            _troop.Agent.velocity = Vector3.zero;
        }
    }

    public void Update()
    {
        // CHECK IF THIS TROOP IS DEAD
        if (_troop == null || _troop.IsDead)
        {
            return;
        }

        // ROBUST NULL AND DEAD CHECKS FOR TARGET
        if (_troop.Target == null || _troop.Target.IsDead || _troop.Target.gameObject == null)
        {
            _troop.SetTarget(null);
            return;
        }

        // Check if target moved out of range
        if (!_troop.IsInRange(_troop.Target))
        {
            _troop.SetTarget(_troop.Target); // Switch back to MoveState
            return;
        }

        // Attack timer countdown
        _attackTimer -= Time.deltaTime;

        if (_attackTimer <= 0f)
        {
            _troop.Attack();

            if (_isFirstAttack)
            {
                _isFirstAttack = false;
            }

            // Use Troop_Atoms helper method - NO TroopStats reference
            _attackTimer = _troop.GetAttackCooldown();
        }
    }

    public void Exit()
    {
        // Resume agent movement
        if (_troop != null && _troop.Agent != null && _troop.Agent.isOnNavMesh && _troop.Agent.enabled)
        {
            _troop.Agent.isStopped = false;
        }
    }
}