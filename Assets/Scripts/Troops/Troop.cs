using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Troop : MonoBehaviour, ITroop, IEquatable<Troop>
{
    public TroopStats TroopStats;
    [HideInInspector] public int CurrentHealth;
    [HideInInspector] public Troop Target;
    [HideInInspector] public int TeamIndex;
    [HideInInspector] public bool IsDead = false;
    [HideInInspector] public bool IsAIActive = false;

    private NavMeshAgent _agent;
    private Animator _animator = null;
    private TroopFSM _fsm;
    protected TroopAnimationController _animController;
    private float _lastAttackTime = float.MinValue; // ADDED: Initialize to very small value

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
    
    int ITroop.TeamIndex // CHANGED: Property instead of Guid
    {
        get => TeamIndex;
        set => TeamIndex = value;
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

    // NEW: Direct stat access (just forward to TroopStats)
    int ITroop.GetMaxHealth() => TroopStats?.MaxHealth ?? 100;
    int ITroop.GetDamage() => TroopStats?.Damage ?? 10;
    float ITroop.GetAttackRange() => TroopStats?.AttackRange ?? 2f;
    float ITroop.GetAttackCooldown() => TroopStats?.AttackCooldown ?? 1f;
    float ITroop.GetMoveSpeed() => TroopStats?.MoveSpeed ?? 3.5f;
    float ITroop.GetInitialAttackDelay() => TroopStats?.InitialAttackDelay ?? 0.3f;

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

        // NEW: Add team indicator
        AddTeamIndicator();

        OnStart();
    }

    private void AddTeamIndicator()
    {
        // Check if already has indicator
        if (GetComponentInChildren<UnitTeamIndicator>() != null)
            return;

        // Create indicator GameObject
        GameObject indicator = new GameObject("TeamIndicator");
        indicator.transform.SetParent(transform);
        indicator.transform.localPosition = Vector3.zero;
        indicator.transform.localRotation = Quaternion.identity;
        
        // Add components
        indicator.AddComponent<SpriteRenderer>();
        indicator.AddComponent<UnitTeamIndicator>();
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
        
        IsDead = true;
        IsAIActive = false;
        
        // Disable collider - NO NULL CONDITIONAL
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
            Debug.Log($"[Troop] {name} disabled collider");
        }
        
        // Hide visuals
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
        
        // Raise event
        Debug.Log($"[Troop] {name} raising UnitDied event");
        GlobalEvents.RaiseUnitDied(this);
        
        StartCoroutine(DestroyAfterDelay());
    }

    private System.Collections.IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        
        // Record death
        if (PerformanceProfiler.Instance != null)
        {
            PerformanceProfiler.Instance.RecordUnitDeath();
        }
        
        Destroy(gameObject);
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

        int damage = TroopStats.Damage;
        Target.TakeDamage(damage);
        
        // Record attack
        if (PerformanceProfiler.Instance != null)
        {
            PerformanceProfiler.Instance.RecordAttack();
            PerformanceProfiler.Instance.RecordDamage(damage);
        }
        
        // FIXED: Only update _lastAttackTime once
        _lastAttackTime = Time.time;
    }

    // ADDED: Public accessor for last attack time (used by FSM)
    public float GetLastAttackTime()
    {
        return _lastAttackTime;
    }

    // ADDED: Check if cooldown has elapsed
    public bool CanAttack()
    {
        return Time.time - _lastAttackTime >= TroopStats.AttackCooldown;
    }

    public bool Equals(Troop other)
    {
        if (other == null) return false;
        return this.GetInstanceID() == other.GetInstanceID();
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as Troop);
    }

    public override int GetHashCode()
    {
        return GetInstanceID().GetHashCode();
    }
}