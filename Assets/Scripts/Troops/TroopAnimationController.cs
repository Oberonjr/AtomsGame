using UnityEngine;

public class TroopAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator _animator;
    private ITroop _troop;

    [Header("Movement Animation Parameters (Bools)")]
    [SerializeField] private string _runParameterName = "isRunning";
    [SerializeField] private string _walkParameterName = "isWalking";

    [Header("Melee Animation Parameters (Triggers)")]
    [SerializeField] private string _attackThrustParameterName = "AttackThrust";
    [SerializeField] private string _attackSlashParameterName = "AttackSlash";
    private bool _useThrust = true;

    [Header("Ranged Animation Parameters")]
    [SerializeField] private string _aimedShotParameterName = "AimedShot";
    [SerializeField] private string _hipFireParameterName = "HipFire";
    [SerializeField] private float _closeRangeDistance = 7f;

    [Header("Artillery Animation Parameters")]
    [SerializeField] private string _aimRocketParameterName = "AimRocket";
    [SerializeField] private string _fireRocketParameterName = "FireRocket";
    [SerializeField] private float _aimDelay = 0.5f;

    [Header("Common Animation Parameters (Triggers)")]
    [SerializeField] private string _hitParameterName = "GetHit";

    void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        _troop = GetComponent<ITroop>() as ITroop;
        
        if (_troop == null)
        {
            Debug.LogError($"[TroopAnimationController] {name} has no ITroop component!");
        }

        ValidateAnimatorParameters();
    }
    
    void Update()
    {
        if (_animator != null && _troop != null)
        {
            UpdateMovementAnimation();
        }
    }
    
    private void ValidateAnimatorParameters()
    {
        if (_animator == null)
        {
            Debug.LogError($"[AnimController] {gameObject.name} has no Animator!");
            return;
        }
    }

    private void UpdateMovementAnimation()
    {
        if (_troop.IsDead) return;
        
        UnityEngine.AI.NavMeshAgent agent = null;
        
        if (_troop is Troop unityTroop)
        {
            agent = unityTroop.Agent;
        }
        else if (_troop is Troop_Atoms atomsTroop)
        {
            agent = atomsTroop.Agent;
        }
        
        if (agent == null || !agent.isOnNavMesh) return;

        bool isMoving = agent.velocity.sqrMagnitude > 0.1f;

        if (HasParameter(_runParameterName))
        {
            _animator.SetBool(_runParameterName, isMoving);
        }

        if (HasParameter(_walkParameterName))
        {
            _animator.SetBool(_walkParameterName, isMoving);
        }
    }
    
    public void PlayAttackAnimation()
    {
        if (_animator == null || _troop == null || _troop.IsDead) return;

        TroopType type = TroopType.MELEE;
        
        if (_troop is Troop unityTroop && unityTroop.TroopStats != null)
        {
            type = unityTroop.TroopStats.TroopType;
        }
        else if (_troop is Troop_Atoms atomsTroop)
        {
            type = atomsTroop.Stats.TroopType;
        }

        switch (type)
        {
            case TroopType.MELEE:
                PlayMeleeAttack();
                break;
            case TroopType.RANGED:
                PlayRangedAttack();
                break;
            case TroopType.ARTILLERY:
                PlayArtilleryAttack();
                break;
        }
    }

    private void PlayMeleeAttack()
    {
        string trigger = _useThrust ? _attackThrustParameterName : _attackSlashParameterName;
        
        if (HasParameter(trigger))
        {
            // ? SECTION 14: Record animation trigger
            if (PerformanceProfiler.Instance != null)
            {
                PerformanceProfiler.Instance.RecordAnimationTrigger($"Melee_{trigger}");
            }
            
            _animator.SetTrigger(trigger);
            _useThrust = !_useThrust;
        }
        else
        {
            Debug.LogError($"[AnimController] Missing trigger parameter: {trigger}");
        }
    }

    private void PlayRangedAttack()
    {
        bool isClose = _troop.Target != null &&
                       Vector3.Distance(_troop.Transform.position, _troop.Target.Transform.position) < _closeRangeDistance;
        
        string trigger = isClose ? _hipFireParameterName : _aimedShotParameterName;
        
        if (HasParameter(trigger))
        {
            // ? SECTION 14: Record animation trigger
            if (PerformanceProfiler.Instance != null)
            {
                PerformanceProfiler.Instance.RecordAnimationTrigger($"Ranged_{trigger}");
            }
            
            _animator.SetTrigger(trigger);
        }
        else
        {
            Debug.LogError($"[AnimController] Missing trigger parameter: {trigger}");
        }
    }

    private void PlayArtilleryAttack()
    {
        if (HasParameter(_aimRocketParameterName))
        {
            // ? SECTION 14: Record animation trigger
            if (PerformanceProfiler.Instance != null)
            {
                PerformanceProfiler.Instance.RecordAnimationTrigger($"Artillery_Aim");
            }
            
            _animator.SetTrigger(_aimRocketParameterName);
            Invoke(nameof(PlayFireRocket), _aimDelay);
        }
        else
        {
            Debug.LogError($"[AnimController] Missing trigger parameter: {_aimRocketParameterName}");
        }
    }

    private void PlayFireRocket()
    {
        if (_animator != null)
        {
            if (HasParameter(_fireRocketParameterName))
            {
                // ? SECTION 14: Record animation trigger
                if (PerformanceProfiler.Instance != null)
                {
                    PerformanceProfiler.Instance.RecordAnimationTrigger($"Artillery_Fire");
                }
                
                _animator.SetTrigger(_fireRocketParameterName);
            }
            else
            {
                Debug.LogError($"[AnimController] Missing trigger parameter: {_fireRocketParameterName}");
            }
        }
    }

    public void PlayHitAnimation()
    {
        if (_animator != null)
        {
            if (HasParameter(_hitParameterName))
            {
                // ? SECTION 14: Record animation trigger
                if (PerformanceProfiler.Instance != null)
                {
                    PerformanceProfiler.Instance.RecordAnimationTrigger("GetHit");
                }
                
                _animator.SetTrigger(_hitParameterName);
            }
            else
            {
                Debug.LogError($"[AnimController] Missing trigger parameter: {_hitParameterName}");
            }
        }
    }

    private bool HasParameter(string paramName)
    {
        if (_animator == null) return false;
        
        foreach (AnimatorControllerParameter param in _animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }
}