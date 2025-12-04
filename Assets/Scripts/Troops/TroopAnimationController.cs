using UnityEngine;

public class TroopAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator _animator;
    private Troop _troop;

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
            _animator = GetComponent<Animator>();
        _troop = GetComponent<Troop>();
        
        // Validate animator parameters
        ValidateAnimatorParameters();
    }

    void Update()
    {
        if (_animator == null || _troop == null) return;

        UpdateMovementAnimation();
    }

    private void ValidateAnimatorParameters()
    {
        if (_animator == null)
        {
            Debug.LogError($"[AnimController] {gameObject.name} has no Animator!");
            return;
        }

        // Log all available parameters
        Debug.Log($"[AnimController] {gameObject.name} Animator parameters:");
        foreach (AnimatorControllerParameter param in _animator.parameters)
        {
            Debug.Log($"  - {param.name} ({param.type})");
        }
    }

    private void UpdateMovementAnimation()
    {
        if (_troop.Agent == null) return;

        bool isMoving = _troop.Agent.velocity.sqrMagnitude > 0.01f;

        switch (_troop.TroopStats.TroopType)
        {
            case TroopType.ARTILLERY:
                if (HasParameter(_walkParameterName))
                    _animator.SetBool(_walkParameterName, isMoving);
                else
                    Debug.LogWarning($"[AnimController] Missing parameter: {_walkParameterName}");
                break;
            default:
                if (HasParameter(_runParameterName))
                    _animator.SetBool(_runParameterName, isMoving);
                else
                    Debug.LogWarning($"[AnimController] Missing parameter: {_runParameterName}");
                break;
        }
    }

    public void PlayAttackAnimation()
    {
        if (_animator == null) return;

        switch (_troop.TroopStats.TroopType)
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
        string attackParam = _useThrust ? _attackThrustParameterName : _attackSlashParameterName;
        Debug.Log($"[AnimController] Playing melee attack: {attackParam}");
        
        if (HasParameter(attackParam))
        {
            _animator.SetTrigger(attackParam);
            _useThrust = !_useThrust;
        }
        else
        {
            Debug.LogError($"[AnimController] Missing trigger parameter: {attackParam}");
        }
    }

    private void PlayRangedAttack()
    {
        if (_troop.Target == null) return;

        bool isClose = Vector3.Distance(_troop.transform.position, _troop.Target.transform.position) < _closeRangeDistance;
        string attackParam = isClose ? _hipFireParameterName : _aimedShotParameterName;
        
        Debug.Log($"[AnimController] Playing ranged attack: {attackParam}");
        
        if (HasParameter(attackParam))
        {
            _animator.SetTrigger(attackParam);
        }
        else
        {
            Debug.LogError($"[AnimController] Missing trigger parameter: {attackParam}");
        }
    }

    private void PlayArtilleryAttack()
    {
        Debug.Log($"[AnimController] Playing artillery attack: {_aimRocketParameterName}");
        
        if (HasParameter(_aimRocketParameterName))
        {
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
            Debug.Log($"[AnimController] Playing fire rocket: {_fireRocketParameterName}");
            
            if (HasParameter(_fireRocketParameterName))
            {
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
            Debug.Log($"[AnimController] Playing hit animation: {_hitParameterName}");
            
            if (HasParameter(_hitParameterName))
            {
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