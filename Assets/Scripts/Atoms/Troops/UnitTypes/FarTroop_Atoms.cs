using UnityEngine;
using UnityAtoms.BaseAtoms;

/// <summary>
/// Atoms ranged troop - retreats if too close, shoots with raycast
/// </summary>
public class FarTroop_Atoms : Troop_Atoms
{
    [Header("Ranged Settings - Atoms Variables")]
    [SerializeField] private FloatVariable _closeRangeDistance;
    [SerializeField] private FloatVariable _retreatSpeed;
    [SerializeField] private FloatVariable _lineDuration;
    
    [Header("References")]
    public Transform FirePoint;
    public GameObject HitEffectPrefab;
    public LineRenderer LineRenderer;

    private float _lineTimer;
    
    // Cached values for performance
    private float _cachedCloseRange;
    private float _cachedRetreatSpeed;
    private float _cachedLineDuration;

    protected override void OnStart()
    {
        base.OnStart();
        
        // Cache Atoms variable values
        _cachedCloseRange = _closeRangeDistance?.Value ?? 7f;
        _cachedRetreatSpeed = _retreatSpeed?.Value ?? 3f;
        _cachedLineDuration = _lineDuration?.Value ?? 0.1f;
        
        // Subscribe to changes
        _closeRangeDistance?.Changed.Register(OnCloseRangeChanged);
        _retreatSpeed?.Changed.Register(OnRetreatSpeedChanged);
        _lineDuration?.Changed.Register(OnLineDurationChanged);
        
        // Setup LineRenderer
        if (LineRenderer == null)
        {
            LineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        LineRenderer.startWidth = 0.05f;
        LineRenderer.endWidth = 0.05f;
        LineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        LineRenderer.startColor = Color.yellow;
        LineRenderer.endColor = Color.red;
        LineRenderer.enabled = false;
    }
    
    void OnDestroy()
    {
        // Unsubscribe
        _closeRangeDistance?.Changed.Unregister(OnCloseRangeChanged);
        _retreatSpeed?.Changed.Unregister(OnRetreatSpeedChanged);
        _lineDuration?.Changed.Unregister(OnLineDurationChanged);
    }
    
    // Atoms event handlers
    private void OnCloseRangeChanged(float newValue) => _cachedCloseRange = newValue;
    private void OnRetreatSpeedChanged(float newValue) => _cachedRetreatSpeed = newValue;
    private void OnLineDurationChanged(float newValue) => _cachedLineDuration = newValue;

    protected override void OnUpdate()
    {
        base.OnUpdate();
        
        // Fade out line
        if (_lineTimer > 0)
        {
            _lineTimer -= Time.deltaTime;
            if (_lineTimer <= 0)
            {
                LineRenderer.enabled = false;
            }
        }
    }

    public override void Attack()
    {
        if (IsDead) return;
        if (Target == null || Target.IsDead) return;

        float distance = Vector3.Distance(transform.position, Target.transform.position);
        bool isClose = distance < _cachedCloseRange;

        // If too close, retreat while shooting
        if (isClose && Agent != null && Agent.isOnNavMesh && Agent.enabled)
        {
            Vector3 retreatDirection = (transform.position - Target.transform.position).normalized;
            Vector3 retreatTarget = transform.position + retreatDirection * _cachedRetreatSpeed;
            retreatTarget.z = 0f;

            if (UnityEngine.AI.NavMesh.SamplePosition(retreatTarget, out UnityEngine.AI.NavMeshHit hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
            {
                Agent.SetDestination(hit.position);
                Agent.isStopped = false;
            }
        }
        else
        {
            if (Agent != null && Agent.isOnNavMesh && Agent.enabled)
            {
                Agent.isStopped = true;
            }
        }

        // Perform raycast attack
        PerformRaycastAttack();
    }

    private void PerformRaycastAttack()
    {
        if (Target == null || Target.IsDead) return;

        if(_animController != null)
        {
            _animController.PlayAttackAnimation();
        }

        Vector3 firePosition = FirePoint != null ? FirePoint.position : transform.position;
        Vector3 direction = (Target.transform.position - firePosition).normalized;

        // Draw line
        if (LineRenderer != null)
        {
            LineRenderer.SetPosition(0, firePosition);
            LineRenderer.SetPosition(1, Target.transform.position);
            LineRenderer.enabled = true;
            _lineTimer = _cachedLineDuration;
        }

        // Raycast
        RaycastHit2D rayHit = Physics2D.Raycast(firePosition, direction, GetAttackRange());

        if (rayHit.collider != null)
        {
            Troop_Atoms hitTroop = rayHit.collider.GetComponentInParent<Troop_Atoms>();
            if (hitTroop != null && hitTroop.TeamIndex != TeamIndex && !hitTroop.IsDead)
            {
                hitTroop.TakeDamage(GetDamage());

                if (HitEffectPrefab != null)
                {
                    Instantiate(HitEffectPrefab, rayHit.point, Quaternion.identity);
                }
            }
        }
    }
}