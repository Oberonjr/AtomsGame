using System.Collections;
using UnityEngine;

/// <summary>
/// Atoms homing rocket projectile - inherits values from ArtilleryTroop_Atoms
/// No per-rocket Atoms variables needed - values are passed in Initialize()
/// </summary>
public class Rocket_Atoms : MonoBehaviour
{
    private Troop_Atoms _target;
    private int _damage;
    private int _ownerTeamIndex;
    
    // All these values are inherited from ArtilleryTroop_Atoms via Initialize()
    private float _initialStraightTime;
    private float _initialSpeed;
    private float _maxSpeed;
    private float _acceleration;
    private float _initialTurnSpeed;
    private float _maxTurnSpeed;
    private float _turnAcceleration;
    private float _explosionRadius;
    private float _lifespan;
    private GameObject _explosionEffectPrefab;
    private float _vfxDesignRadius;
    
    // Runtime state (no need for Atoms variables - just local values)
    private float _currentSpeed;
    private float _currentTurnSpeed;
    private float _elapsedTime;
    private Vector3 _velocity;
    
    [Header("Collision Settings")]
    [SerializeField] private LayerMask _targetLayers; // NEW: Only hit specific layers
    
    public void Initialize(
        Troop_Atoms target,
        int damage,
        int ownerTeamIndex,
        float initialStraightTime,
        float initialSpeed,
        float maxSpeed,
        float acceleration,
        float initialTurnSpeed,
        float maxTurnSpeed,
        float turnAcceleration,
        float explosionRadius,
        float lifespan,
        GameObject explosionEffectPrefab,
        float vfxDesignRadius)
    {
        _target = target;
        _damage = damage;
        _ownerTeamIndex = ownerTeamIndex;
        _initialStraightTime = initialStraightTime;
        _initialSpeed = initialSpeed;
        _maxSpeed = maxSpeed;
        _acceleration = acceleration;
        _initialTurnSpeed = initialTurnSpeed;
        _maxTurnSpeed = maxTurnSpeed;
        _turnAcceleration = turnAcceleration;
        _explosionRadius = explosionRadius;
        _lifespan = lifespan;
        _explosionEffectPrefab = explosionEffectPrefab;
        _vfxDesignRadius = vfxDesignRadius;
        
        // Initialize runtime state
        _currentSpeed = _initialSpeed;
        _currentTurnSpeed = _initialTurnSpeed;
        _elapsedTime = 0f;
        _velocity = transform.right * _currentSpeed;
        
        StartCoroutine(LifespanCoroutine());
    }
    
    void Start()
    {
        // Ensure rocket doesn't collide with other projectiles
        // Put rockets on "Projectile" layer and exclude it from targets
        gameObject.layer = LayerMask.NameToLayer("Projectile");
        
        // If _targetLayers not set in inspector, default to hitting everything except Projectile
        if (_targetLayers == 0)
        {
            _targetLayers = ~(1 << LayerMask.NameToLayer("Projectile"));
        }
    }
    
    void Update()
    {
        if (_target == null || _target.IsDead)
        {
            Explode();
            return;
        }
        
        _elapsedTime += Time.deltaTime;
        
        // Accelerate speed
        if (_currentSpeed < _maxSpeed)
        {
            _currentSpeed += _acceleration * Time.deltaTime;
            _currentSpeed = Mathf.Min(_currentSpeed, _maxSpeed);
        }
        
        // Accelerate turn speed
        if (_currentTurnSpeed < _maxTurnSpeed)
        {
            _currentTurnSpeed += _turnAcceleration * Time.deltaTime;
            _currentTurnSpeed = Mathf.Min(_currentTurnSpeed, _maxTurnSpeed);
        }
        
        // Straight phase
        if (_elapsedTime < _initialStraightTime)
        {
            _velocity = transform.right * _currentSpeed;
        }
        // Homing phase
        else
        {
            Vector3 directionToTarget = (_target.transform.position - transform.position).normalized;
            float targetAngle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
            float currentAngle = transform.eulerAngles.z;
            
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, _currentTurnSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0, 0, newAngle);
            
            _velocity = transform.right * _currentSpeed;
        }
        
        transform.position += _velocity * Time.deltaTime;
        
        Vector3 pos = transform.position;
        pos.z = 0f;
        transform.position = pos;
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // CHANGED: Check if the collided object is on a valid target layer
        if ((_targetLayers.value & (1 << other.gameObject.layer)) == 0)
        {
            // Not a valid target layer, ignore
            return;
        }
        
        Troop_Atoms hitTroop = other.GetComponentInParent<Troop_Atoms>();
        
        if (hitTroop != null && hitTroop.TeamIndex != _ownerTeamIndex && !hitTroop.IsDead)
        {
            DealDamageInRadius(transform.position);
            Explode();
        }
    }
    
    private void DealDamageInRadius(Vector3 center)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, _explosionRadius);
        
        foreach (Collider2D hit in hits)
        {
            Troop_Atoms troop = hit.GetComponentInParent<Troop_Atoms>();
            if (troop != null && troop.TeamIndex != _ownerTeamIndex && !troop.IsDead)
            {
                troop.TakeDamage(_damage);
            }
        }
    }
    
    private void Explode()
    {
        if (_explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
            float scaleFactor = _explosionRadius / _vfxDesignRadius;
            effect.transform.localScale = Vector3.one * scaleFactor;
        }
        
        Destroy(gameObject);
    }
    
    private IEnumerator LifespanCoroutine()
    {
        yield return new WaitForSeconds(_lifespan);
        Explode();
    }
}