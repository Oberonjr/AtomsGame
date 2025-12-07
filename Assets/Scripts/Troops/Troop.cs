using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Troop : MonoBehaviour, ITroop
{
    public TroopStats TroopStats;
    [HideInInspector] public int CurrentHealth;
    [HideInInspector] public Troop Target;
    [HideInInspector] public int TeamIndex; // CHANGED: From Guid to int
    [HideInInspector] public bool IsDead = false;
    [HideInInspector] public bool IsAIActive = false; // NEW: Controls AI behavior

    private NavMeshAgent _agent;
    private Animator _animator = null;
    private TroopFSM _fsm;
    protected TroopAnimationController _animController;

    public NavMeshAgent Agent => _agent;
    public Animator Animator => _animator;
    public TroopFSM FSM => _fsm;

    // IMPLEMENT ITroop INTERFACE
    GameObject ITroop.GameObject
    {
        get
        {
            // Safe access - return null if destroyed
            if (this == null) return null;
            try
            {
                return gameObject;
            }
            catch (MissingReferenceException)
            {
                return null;
            }
        }
    }

    Transform ITroop.Transform
    {
        get
        {
            // Safe access - return null if destroyed
            if (this == null) return null;
            try
            {
                return transform;
            }
            catch (MissingReferenceException)
            {
                return null;
            }
        }
    }
    Guid ITroop.TeamID 
    { 
        get => Guid.Empty; // Deprecated
        set { } // Deprecated
    }
    TroopStats ITroop.TroopStats => TroopStats;
    int ITroop.CurrentHealth
    {
        get => CurrentHealth;
        set => CurrentHealth = value;
    }
    bool ITroop.IsDead => IsDead;
    ITroop ITroop.Target
    {
        get => Target;
        set => Target = value as Troop;
    }

    void ITroop.Initialize()
    {
        // Enable AI instead of enabling the script
        IsAIActive = true;

        // Reset agent in case it was stopped during prep
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
        }
    }

    void ITroop.UpdateTroop()
    {
        if (_fsm != null)
            _fsm.Update();

        OnUpdate();
    }

    void ITroop.SetTarget(ITroop target)
    {
        SetTarget(target as Troop);
    }

    bool ITroop.IsInRange(ITroop target)
    {
        return IsInRange(target as Troop);
    }

    void ITroop.Attack()
    {
        Attack();
    }

    void ITroop.TakeDamage(int damage)
    {
        TakeDamage(damage);
    }

    void ITroop.Die()
    {
        Die();
    }

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.updateRotation = false;
        _agent.updateUpAxis = false;
        transform.rotation = Quaternion.identity;
        
        // Initialize FSM immediately
        _fsm = new TroopFSM();
        _fsm.ChangeState(new IdleState(this));
        
        Debug.Log($"[Troop] {name} FSM initialized in Awake");
    }

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        
        // SAVE original rotation before NavMesh changes it
        Quaternion originalRotation = transform.rotation;
        
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

        // FSM ALREADY INITIALIZED IN AWAKE
        // _fsm = new TroopFSM(); // REMOVE THIS
        // _fsm.ChangeState(new IdleState(this)); // REMOVE THIS

        _animController = GetComponent<TroopAnimationController>();

        // RESTORE original rotation after NavMesh setup
        transform.rotation = originalRotation;

        OnStart();
    }

    protected virtual void OnStart()
    {
    }

    void Update()
    {
        // Don't update FSM if dead
        if (IsDead) return;
        
        // ONLY update FSM if AI is active
        if (IsAIActive)
        {
            // ENSURE FSM EXISTS
            EnsureFSMInitialized();
            
            if (_fsm != null)
            {
                _fsm.Update();
            }
        }

        OnUpdate();
    }

    private void EnsureFSMInitialized()
    {
        if (_fsm == null)
        {
            _fsm = new TroopFSM();
            _fsm.ChangeState(new IdleState(this));
            Debug.Log($"[Troop] {name} FSM initialized on demand");
        }
    }

    protected virtual void OnUpdate()
    {
    }

    void LateUpdate()
    {
        // Don't update position/rotation if dead
        if (IsDead) return;
        
        Vector3 pos = transform.position;
        pos.z = 0f;
        transform.position = pos;

        Vector3 euler = transform.eulerAngles;
        euler.x = 0f;
        euler.y = 0f;

        // ONLY rotate to face target if AI is active
        if (IsAIActive && Target != null && Target.gameObject != null)
        {
            Vector3 direction = (Target.transform.position - transform.position).normalized;
            if (direction.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                euler.z = angle;
            }
        }
        else if (IsAIActive && _agent != null && _agent.enabled && _agent.isOnNavMesh && _agent.velocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(_agent.velocity.y, _agent.velocity.x) * Mathf.Rad2Deg;
            euler.z = angle;
        }

        transform.eulerAngles = euler;
    }

    public void SetTarget(Troop target)
    {
        // Don't accept dead targets
        if (target != null && (target.IsDead || target.gameObject == null))
        {
            Debug.LogWarning($"[Troop] {name} tried to set dead target {target.name}");
            target = null;
        }
        
        Target = target;
        
        // ONLY change state if AI is active
        if (!IsAIActive) return;
        
        // ENSURE FSM EXISTS
        EnsureFSMInitialized();
        
        if (_fsm == null)
        {
            Debug.LogWarning($"[Troop] {name} FSM is null, cannot set target");
            return;
        }
        
        if (Target != null)
        {
            if (IsInRange(Target))
                _fsm.ChangeState(new AttackState(this));
            else
                _fsm.ChangeState(new MoveState(this));
        }
        else
        {
            _fsm.ChangeState(new IdleState(this));
        }
    }

    public bool IsInRange(Troop target)
    {
        if (target == null) return false;
        return Vector3.Distance(transform.position, target.transform.position) <= TroopStats.AttackRange;
    }

    public void TakeDamage(int damage)
    {
        if (IsDead)
        {
            return; // Silently ignore damage to dead troops
        }
        
        CurrentHealth -= damage;
        
        if (_animController != null)
        {
            _animController.PlayHitAnimation();
        }
        
        if (CurrentHealth <= 0)
        {
            Die(); // Call Die() - it will set IsDead
        }
    }

    public void Die()
    {
        if (gameObject == null)
        {
            Debug.LogError("[Troop] Die() called but gameObject is null!");
            return;
        }
        
        Debug.Log($"[Troop] {name} is dying. Health: {CurrentHealth}");
        
        IsDead = true; // Set it HERE, not before calling Die()
        
        // IMMEDIATELY disable AI
        IsAIActive = false;
        
        // IMMEDIATELY disable collider so it can't be targeted
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
            Debug.Log($"[Troop] {name} disabled collider");
        }
        
        // IMMEDIATELY hide visuals
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
        Debug.Log($"[Troop] {name} disabled {renderers.Length} renderers");
        
        // Stop agent
        if (Agent != null && Agent.enabled && Agent.isOnNavMesh)
        {
            Agent.isStopped = true;
            Agent.velocity = Vector3.zero;
            Agent.enabled = false;
            Debug.Log($"[Troop] {name} disabled agent");
        }
        
        // Raise event BEFORE destroying
        Debug.Log($"[Troop] {name} raising UnitDied event");
        GlobalEvents.RaiseUnitDied(this);
        
        // Delay destruction slightly to ensure event is processed
        StartCoroutine(DestroyAfterDelay());
    }

    private System.Collections.IEnumerator DestroyAfterDelay()
    {
        yield return null; // Wait one frame
        Debug.Log($"[Troop] {name} destroying GameObject now");
        
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    public virtual void Attack()
    {
        // ROBUST NULL AND DEAD CHECKS
        if (Target == null || Target.IsDead || Target.gameObject == null)
        {
            Debug.LogWarning($"[Troop] {name} tried to attack invalid target");
            return;
        }

        if (_animController != null)
        {
            _animController.PlayAttackAnimation();
        }

        Target.TakeDamage(TroopStats.Damage);
    }
}