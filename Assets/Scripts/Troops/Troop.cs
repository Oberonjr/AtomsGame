using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Troop : MonoBehaviour
{
    public TroopStats TroopStats;
    [HideInInspector] public int CurrentHealth;
    [HideInInspector] public Troop Target;
    [HideInInspector] public Guid TeamID;

    private NavMeshAgent _agent;
    private Animator _animator = null;
    private TroopFSM _fsm;
    private TroopAnimationController _animController;

    public NavMeshAgent Agent => _agent;
    public Animator Animator => _animator;
    public TroopFSM FSM => _fsm;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();

        // CRITICAL: Configure agent for 2D BEFORE checking if on NavMesh
        _agent.updateRotation = false;
        _agent.updateUpAxis = false;
        transform.rotation = Quaternion.identity;
    }

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        
        //// CRITICAL: Configure agent for 2D
        //_agent.updateRotation = false;
        //_agent.updateUpAxis = false;
        
        Vector3 pos = transform.position;
        pos.z = 0f;
        transform.position = pos;
        
        // Check if on NavMesh
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

        // Configure NavMesh avoidance
        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        _agent.avoidancePriority = 50;
        _agent.radius = 0.5f;

        // Initialize FSM - THIS IS CRITICAL
        _fsm = new TroopFSM();
        _fsm.ChangeState(new IdleState(this));

        // Get animation controller
        _animController = GetComponent<TroopAnimationController>();

        // Call subclass initialization
        OnStart();
        
        Debug.Log($"[Troop] {name} initialized with FSM in state: {_fsm.CurrentState?.GetType().Name}");
    }

    // Virtual method for subclasses to override
    protected virtual void OnStart()
    {
        // Override in subclasses if needed
    }

    void Update()
    {
        if (_fsm != null)
            _fsm.Update();
    }

    void LateUpdate()
    {
        // AGGRESSIVELY lock Z position and rotation for 2D
        Vector3 pos = transform.position;
        pos.z = 0f;
        transform.position = pos;
        
        // Lock X and Y rotation to 0
        Vector3 euler = transform.eulerAngles;
        euler.x = 0f;
        euler.y = 0f;
        
        // ALWAYS face target if we have one, regardless of state
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
            // Face movement direction when moving and no target
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
            // Play attack animation
            if (_animController != null)
            {
                _animController.PlayAttackAnimation();
            }
            
            Target.TakeDamage(TroopStats.Damage);
        }
    }

    public void TakeDamage(int damage)
    {
        CurrentHealth -= damage;
        
        // Play hit animation
        if (_animController != null)
        {
            _animController.PlayHitAnimation();
        }
        
        if (CurrentHealth <= 0)
        {
            Die();
        }
    }
}