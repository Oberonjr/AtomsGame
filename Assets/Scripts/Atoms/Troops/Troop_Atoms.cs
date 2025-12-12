using System;
using UnityEngine;
using UnityEngine.AI;
using UnityAtoms;
using UnityAtoms.BaseAtoms;

/// <summary>
/// Pure Atoms troop implementation with per-instance variables using Instancers
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Troop_Atoms : MonoBehaviour, ITroop, IEquatable<Troop_Atoms>
{
    [Header("Atoms Stats Configuration")]
    [Tooltip("Atoms stats struct - the single source of truth")]
    [SerializeField] private TroopStats_AtomsVariableInstancer _statsInstancer;
    
    [Header("Per-Instance Atoms Variables")]
    [SerializeField] private IntVariableInstancer _healthInstancer;
    [SerializeField] private BoolVariableInstancer _isDeadInstancer;
    [SerializeField] private BoolVariableInstancer _isAIActiveInstancer;
    
    [Header("Atoms Events")]
    [SerializeField] private IntEvent _onDamageTaken;
    [SerializeField] private VoidEvent _onDeath;
    [SerializeField] private VoidEvent _onTargetAcquired;
    [SerializeField] private VoidEvent _onTargetLost;
    
    [Header("Traditional Fields")]
    public int TeamIndex;
    
    private NavMeshAgent _agent;
    private Animator _animator;
    private TroopFSM_Atoms _fsm;
    protected TroopAnimationController _animController;
    protected float _lastAttackTime = float.MinValue;
    private Troop_Atoms _target;
    
    // Cached stats for performance
    private TroopStats_Atoms _cachedStats;
    
    // Properties
    public NavMeshAgent Agent => _agent;
    public Animator Animator => _animator;
    public TroopFSM_Atoms FSM => _fsm;
    public TroopStats_Atoms Stats
    {
        get
        {
            // ADDED: Track variable read
            AtomsPerformanceTracker.TrackVariableRead();
            
            if (_statsInstancer != null && _statsInstancer.Variable != null)
            {
                return _statsInstancer.Variable.Value;
            }
            
            if (_cachedStats.Equals(default(TroopStats_Atoms)))
            {
                Debug.LogWarning($"[Troop_Atoms] {name} Stats accessed before initialization");
                return new TroopStats_Atoms();
            }
            
            return _cachedStats;
        }
    }
    
    // Reactive properties using Instancers
    public int CurrentHealth
    {
        get
        {
            // ADDED: Track variable read
            AtomsPerformanceTracker.TrackVariableRead();
            return _healthInstancer?.Variable.Value ?? 0;
        }
        set
        {
            // ADDED: Track variable write
            AtomsPerformanceTracker.TrackVariableWrite();
            _healthInstancer?.Variable.SetValue(value);
        }
    }
    
    public bool IsDead
    {
        get
        {
            // ADDED: Track variable read
            AtomsPerformanceTracker.TrackVariableRead();
            return _isDeadInstancer?.Variable.Value ?? false;
        }
        private set
        {
            // ADDED: Track variable write
            AtomsPerformanceTracker.TrackVariableWrite();
            _isDeadInstancer?.Variable.SetValue(value);
        }
    }
    
    public bool IsAIActive
    {
        get
        {
            // ADDED: Track variable read
            AtomsPerformanceTracker.TrackVariableRead();
            return _isAIActiveInstancer?.Variable.Value ?? false;
        }
        set
        {
            // ADDED: Track variable write
            AtomsPerformanceTracker.TrackVariableWrite();
            _isAIActiveInstancer?.Variable.SetValue(value);
        }
    }
    
    public Troop_Atoms Target
    {
        get => _target;
        set
        {
            bool hadTarget = _target != null;
            _target = value;
            bool hasTarget = _target != null;
            
            if (!hadTarget && hasTarget)
            {
                // ADDED: Track event dispatch
                AtomsPerformanceTracker.TrackEventDispatch();
                _onTargetAcquired?.Raise();
            }
            else if (hadTarget && !hasTarget)
            {
                // ADDED: Track event dispatch
                AtomsPerformanceTracker.TrackEventDispatch();
                _onTargetLost?.Raise();
            }
        }
    }
    
    // ========== ITroop Interface (for Manager Compatibility) ==========
    GameObject ITroop.GameObject
    {
        get
        {
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
    
    int ITroop.TeamIndex
    {
        get => TeamIndex;
        set => TeamIndex = value;
    }
    
    TroopStats ITroop.TroopStats => null;

    int ITroop.GetMaxHealth() => AtomsVariableConverter.ToInt(_cachedStats.MaxHealth, 100);
    int ITroop.GetDamage() => AtomsVariableConverter.ToInt(_cachedStats.Damage, 10);
    float ITroop.GetAttackRange() => AtomsVariableConverter.ToFloat(_cachedStats.AttackRange, 2f);
    float ITroop.GetAttackCooldown() => AtomsVariableConverter.ToFloat(_cachedStats.AttackCooldown, 1f);
    float ITroop.GetMoveSpeed() => AtomsVariableConverter.ToFloat(_cachedStats.MoveSpeed, 3.5f);
    float ITroop.GetInitialAttackDelay() => AtomsVariableConverter.ToFloat(_cachedStats.InitialAttackDelay, 0.3f);
    
    int ITroop.CurrentHealth { get => CurrentHealth; set => CurrentHealth = value; }
    bool ITroop.IsDead => IsDead;
    ITroop ITroop.Target { get => Target; set => Target = value as Troop_Atoms; }
    
    void ITroop.Initialize() => Initialize();
    void ITroop.UpdateTroop() => _fsm?.Update();
    void ITroop.SetTarget(ITroop target) => SetTarget(target as Troop_Atoms);
    bool ITroop.IsInRange(ITroop target) => IsInRange(target as Troop_Atoms);
    void ITroop.Attack() => Attack();
    void ITroop.TakeDamage(int damage) => TakeDamage(damage);
    void ITroop.Die() => Die();
    
    // ========== Unity Lifecycle ==========
    void Awake()
    {
        // Cache stats - WITH NULL CHECK
        if (_statsInstancer != null && _statsInstancer.Variable != null)
        {
            // ADDED: Track variable read
            AtomsPerformanceTracker.TrackVariableRead();
            _cachedStats = _statsInstancer.Variable.Value;
        }
        else
        {
            Debug.LogWarning($"[Troop_Atoms] {name} has no stats instancer! Using default values.");
            _cachedStats = new TroopStats_Atoms();
        }
        
        // Setup agent
        _agent = GetComponent<NavMeshAgent>();
        if (_agent != null)
        {
            _agent.updateRotation = false;
            _agent.updateUpAxis = false;
        }
        
        // Initialize FSM
        _fsm = new TroopFSM_Atoms();
        _fsm.ChangeState(new IdleState_Atoms(this));
        
        // Subscribe to Atoms Variable changes - WITH NULL CHECKS
        if (_healthInstancer != null && _healthInstancer.Variable != null)
        {
            // ADDED: Track allocation for listener registration
            AtomsPerformanceTracker.TrackAllocation(64); // Approximate listener size
            _healthInstancer.Variable.Changed.Register(OnHealthChanged);
        }
        else
        {
            Debug.LogWarning($"[Troop_Atoms] {name} has no health instancer!");
        }
        
        if (_isDeadInstancer != null && _isDeadInstancer.Variable != null)
        {
            AtomsPerformanceTracker.TrackAllocation(64);
            _isDeadInstancer.Variable.Changed.Register(OnIsDeadChanged);
        }
        else
        {
            Debug.LogWarning($"[Troop_Atoms] {name} has no isDead instancer!");
        }
        
        if (_isAIActiveInstancer != null && _isAIActiveInstancer.Variable != null)
        {
            AtomsPerformanceTracker.TrackAllocation(64);
            _isAIActiveInstancer.Variable.Changed.Register(OnIsAIActiveChanged);
        }
        else
        {
            Debug.LogWarning($"[Troop_Atoms] {name} has no isAIActive instancer!");
        }
        
        if (_statsInstancer != null && _statsInstancer.Variable != null)
        {
            AtomsPerformanceTracker.TrackAllocation(64);
            _statsInstancer.Variable.Changed.Register(OnStatsChanged);
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe - WITH NULL CHECKS
        if (_healthInstancer != null && _healthInstancer.Variable != null)
        {
            _healthInstancer.Variable.Changed.Unregister(OnHealthChanged);
        }
        if (_isDeadInstancer != null && _isDeadInstancer.Variable != null)
        {
            _isDeadInstancer.Variable.Changed.Unregister(OnIsDeadChanged);
        }
        if (_isAIActiveInstancer != null && _isAIActiveInstancer.Variable != null)
        {
            _isAIActiveInstancer.Variable.Changed.Unregister(OnIsAIActiveChanged);
        }
        if (_statsInstancer != null && _statsInstancer.Variable != null)
        {
            _statsInstancer.Variable.Changed.Unregister(OnStatsChanged);
        }
    }
    
    void Start()
    {
        if (TryGetComponent(out _animator))
            _animator = GetComponent<Animator>();
        
        // Initialize from Atoms stats using converter
        CurrentHealth = AtomsVariableConverter.ToInt(_cachedStats.MaxHealth, 100);
        _agent.speed = AtomsVariableConverter.ToFloat(_cachedStats.MoveSpeed, 3.5f);
        _agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        _agent.avoidancePriority = 50;
        _agent.radius = 0.5f;
        
        _animController = GetComponent<TroopAnimationController>();
        
        AddTeamIndicator();
        
        OnStart();
    }

    private void AddTeamIndicator()
    {
        if (GetComponentInChildren<UnitTeamIndicator>() != null)
            return;

        GameObject indicator = new GameObject("TeamIndicator");
        indicator.transform.SetParent(transform);
        indicator.transform.localPosition = Vector3.zero;
        indicator.transform.localRotation = Quaternion.identity;
        
        indicator.AddComponent<SpriteRenderer>();
        indicator.AddComponent<UnitTeamIndicator>();
    }
    
    void Update()
    {
        if (IsDead) return;
        if (IsAIActive && _fsm != null)
        {
            _fsm.Update();
        }
        
        OnUpdate();
    }
    
    void LateUpdate()
    {
        if (IsDead) return;
        
        Vector3 pos = transform.position;
        pos.z = 0f;
        transform.position = pos;
        
        Vector3 euler = transform.eulerAngles;
        euler.x = 0f;
        euler.y = 0f;
        
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
    
    // ========== Atoms Event Handlers ==========
    private void OnHealthChanged(int newHealth)
    {
        // ADDED: Track listener invocation
        AtomsPerformanceTracker.BeginListenerInvoke();
        // React to health changes (e.g., update UI)
        AtomsPerformanceTracker.EndListenerInvoke();
    }
    
    private void OnIsDeadChanged(bool isDead)
    {
        // ADDED: Track listener invocation
        AtomsPerformanceTracker.BeginListenerInvoke();
        // React to death state changes
        AtomsPerformanceTracker.EndListenerInvoke();
    }
    
    private void OnIsAIActiveChanged(bool isActive)
    {
        // ADDED: Track listener invocation
        AtomsPerformanceTracker.BeginListenerInvoke();
        // React to AI state changes
        AtomsPerformanceTracker.EndListenerInvoke();
    }
    
    private void OnStatsChanged(TroopStats_Atoms newStats)
    {
        // ADDED: Track listener invocation
        AtomsPerformanceTracker.BeginListenerInvoke();
        
        _cachedStats = newStats;
        _agent.speed = AtomsVariableConverter.ToFloat(newStats.MoveSpeed, 3.5f);
        
        if (SimulationConfig.Instance?.VerboseLogging ?? false)
        {
            Debug.Log($"[Troop_Atoms] {name} stats changed - new speed: {_agent.speed}");
            AtomsVariableConverter.DebugLogAtomsStats(newStats, $"[{name}] ");
        }
        
        AtomsPerformanceTracker.EndListenerInvoke();
    }
    
    // ========== Core Methods ==========
    public void Initialize()
    {
        IsAIActive = true;
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
        }
    }
    
    public void TakeDamage(int damage)
    {
        if (IsDead) return;
        
        CurrentHealth -= damage; // Triggers tracking via property setter
        
        // ADDED: Track event dispatch
        if (_onDamageTaken != null)
        {
            AtomsPerformanceTracker.TrackEventDispatch();
            _onDamageTaken.Raise(damage);
        }
        
        _animController?.PlayHitAnimation();
        
        if (CurrentHealth <= 0)
        {
            Die();
        }
    }
    
    public void Die()
    {
        if (gameObject == null) return;
        
        IsDead = true; // Triggers tracking via property setter
        IsAIActive = false; // Triggers tracking via property setter
        
        // ADDED: Track event dispatch
        if (_onDeath != null)
        {
            AtomsPerformanceTracker.TrackEventDispatch();
            _onDeath.Raise();
        }
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }
        
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
        
        if (Agent != null && Agent.enabled)
        {
            Agent.isStopped = true;
            Agent.velocity = Vector3.zero;
            Agent.enabled = false;
        }
        
        GlobalEvents.RaiseUnitDied(this);
        
        StartCoroutine(DestroyAfterDelay());
    }
    
    private System.Collections.IEnumerator DestroyAfterDelay()
    {
        yield return null;
        if (gameObject != null)
        {
            if (PerformanceProfiler.Instance != null)
            {
                PerformanceProfiler.Instance.RecordUnitDeath();
            }
            Destroy(gameObject);
        }
    }
    
    public void SetTarget(Troop_Atoms target)
    {
        if (target != null && (target.IsDead || target.gameObject == null))
        {
            target = null;
        }
        
        Target = target; // Triggers event tracking via property setter
        
        if (!IsAIActive || _fsm == null) return;
        
        if (Target != null)
        {
            if (IsInRange(Target))
                _fsm.ChangeState(new AttackState_Atoms(this));
            else
                _fsm.ChangeState(new MoveState_Atoms(this));
        }
        else
        {
            _fsm.ChangeState(new IdleState_Atoms(this));
        }
    }
    
    public bool IsInRange(Troop_Atoms target)
    {
        if (target == null) return false;
        float _cachedAttackRange = AtomsVariableConverter.ToFloat(_cachedStats.AttackRange);
        return Vector3.Distance(transform.position, target.transform.position) <= _cachedAttackRange;
    }
    
    public virtual void Attack()
    {
        if (Target == null || Target.IsDead || Target.gameObject == null)
        {
            Debug.LogWarning($"[Troop_Atoms] {name} tried to attack invalid target");
            return;
        }

        _animController?.PlayAttackAnimation();
        int _cachedDamage = AtomsVariableConverter.ToInt(_cachedStats.Damage);
        Target.TakeDamage(_cachedDamage);

        if (PerformanceProfiler.Instance != null)
        {
            PerformanceProfiler.Instance.RecordAttack();
            PerformanceProfiler.Instance.RecordDamage(_cachedDamage);
        }

        _lastAttackTime = Time.time;
    }

    public float GetLastAttackTime()
    {
        return _lastAttackTime;
    }

    // ========== Helper Methods for FSM and Subclasses ==========
    public float GetAttackRange() => AtomsVariableConverter.ToFloat(_cachedStats.AttackRange, 2f);
    public float GetInitialAttackDelay() => AtomsVariableConverter.ToFloat(_cachedStats.InitialAttackDelay, 0.3f);
    public float GetAttackCooldown() => AtomsVariableConverter.ToFloat(_cachedStats.AttackCooldown, 1f);
    public int GetDamage() => AtomsVariableConverter.ToInt(_cachedStats.Damage, 10);
    public int GetMaxHealth() => AtomsVariableConverter.ToInt(_cachedStats.MaxHealth, 100);
    public float GetMoveSpeed() => AtomsVariableConverter.ToFloat(_cachedStats.MoveSpeed, 3.5f);
    
    // ========== IEquatable ==========
    public bool Equals(Troop_Atoms other)
    {
        if (other == null) return false;
        return GetInstanceID() == other.GetInstanceID();
    }
    
    public override bool Equals(object obj) => Equals(obj as Troop_Atoms);
    public override int GetHashCode() => GetInstanceID();
    
    // ADD THESE VIRTUAL METHODS
    protected virtual void OnStart()
    {
        // Override in subclasses for custom initialization
    }

    protected virtual void OnUpdate()
    {
        // Override in subclasses for custom per-frame logic
    }
}