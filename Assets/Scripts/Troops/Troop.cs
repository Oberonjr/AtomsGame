using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Troop : MonoBehaviour
{
    public TroopStats TroopStats;
    [HideInInspector] public int CurrentHealth;
    [HideInInspector] public Troop Target;
    [HideInInspector] public Guid TeamID;
    [HideInInspector] public bool IsDead = false; // Add this field

    private NavMeshAgent _agent;
    private Animator _animator = null;
    private TroopFSM _fsm;
    protected TroopAnimationController _animController; // Changed to protected so subclasses can access

    public NavMeshAgent Agent => _agent;
    public Animator Animator => _animator;
    public TroopFSM FSM => _fsm;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.updateRotation = false;
        _agent.updateUpAxis = false;
        transform.rotation = Quaternion.identity;
    }

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        
        Vector3 pos = transform.position;
        pos.z = 0f;
        transform.position = pos;
        
        if (!_agent.isOnNavMesh)
        {
            Debug.LogWarning($"[Troop] {name} spawned off NavMesh at {transform.position}, attempting to warp");
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out UnityEngine.AI.NavMeshHit hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                Vector3 newPos = hit.position;
                newPos.z = 0f;
                transform.position = newPos;
                Debug.Log($"[Troop] Warped to NavMesh position: {transform.position}");
            }
            else
            {
                Debug.LogError($"[Troop] Cannot find NavMesh near {transform.position}! Agent will not function.");
            }
        }
        
        if (TryGetComponent(out _animator))
            _animator = GetComponent<Animator>();
        CurrentHealth = TroopStats.MaxHealth;
        _agent.speed = TroopStats.MoveSpeed;

        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        _agent.avoidancePriority = 50;
        _agent.radius = 0.5f;

        _fsm = new TroopFSM();
        _fsm.ChangeState(new IdleState(this));

        // Get animation controller in base class
        _animController = GetComponent<TroopAnimationController>();

        OnStart();
        
        Debug.Log($"[Troop] {name} initialized with FSM in state: {_fsm.CurrentState?.GetType().Name}");
    }

    protected virtual void OnStart()
    {
    }

    void Update()
    {
        if (_fsm != null)
            _fsm.Update();
        
        OnUpdate();
    }

    protected virtual void OnUpdate()
    {
    }

    void LateUpdate()
    {
        Vector3 pos = transform.position;
        pos.z = 0f;
        transform.position = pos;
        
        Vector3 euler = transform.eulerAngles;
        euler.x = 0f;
        euler.y = 0f;
        
        if (Target != null)
        {
            Vector3 direction = (Target.transform.position - transform.position).normalized;
            if (direction.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                euler.z = angle;
            }
        }
        else if (_agent != null && _agent.isOnNavMesh && _agent.velocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(_agent.velocity.y, _agent.velocity.x) * Mathf.Rad2Deg;
            euler.z = angle;
        }
        
        transform.eulerAngles = euler;
    }

    public void SetTarget(Troop target)
    {
        Target = target;
        if (Target != null)
        {
            if (IsInRange(Target))
                _fsm?.ChangeState(new AttackState(this));
            else
                _fsm?.ChangeState(new MoveState(this));
        }
        else
        {
            _fsm?.ChangeState(new IdleState(this));
        }
    }

    public bool IsInRange(Troop target)
    {
        if (target == null) return false;
        return Vector3.Distance(transform.position, target.transform.position) <= TroopStats.AttackRange;
    }

    public void Die()
    {
        GlobalEvents.RaiseUnitDied(this);
        Destroy(gameObject);
    }

    public virtual void Attack()
    {
        if (Target != null && Target.CurrentHealth > 0)
        {
            if (_animController != null)
            {
                _animController.PlayAttackAnimation();
            }
            
            Target.TakeDamage(TroopStats.Damage);
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return; // Early exit if already dead
        
        CurrentHealth -= damage;
        
        if (_animController != null)
        {
            _animController.PlayHitAnimation();
        }
        
        if (CurrentHealth <= 0 && !IsDead)
        {
            IsDead = true;
            Die();
        }
    }
}