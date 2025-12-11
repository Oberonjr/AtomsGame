using System.Collections;
using UnityEngine;

/// <summary>
/// Universal homing rocket - works with both Unity Troop and Troop_Atoms via ITroop interface
/// Retargets to new enemies if original target dies
/// </summary>
public class Rocket : MonoBehaviour
{
    [Header("Collision Settings")]
    [SerializeField] private LayerMask _targetLayers;

    [Header("Retargeting Settings")]
    [SerializeField] private float _retargetRadius = 10f;
    [SerializeField] private float _retargetInterval = 0.5f;

    private ITroop _target;
    private int _damage;
    private int _ownerTeamIndex;

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

    private float _currentSpeed;
    private float _currentTurnSpeed;
    private float _elapsedTime;
    private bool _isHoming;
    private bool _isDestroyed;
    private float _retargetTimer;

    public void Initialize(
        ITroop target,
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

        _currentSpeed = _initialSpeed;
        _currentTurnSpeed = _initialTurnSpeed;
        _elapsedTime = 0f;
        _isHoming = false;
        _isDestroyed = false;
        _retargetTimer = 0f;
    }

    void Start()
    {
        int projectileLayer = LayerMask.NameToLayer("Projectile");
        if (projectileLayer != -1)
        {
            gameObject.layer = projectileLayer;
        }

        if (_targetLayers == 0)
        {
            _targetLayers = ~(1 << projectileLayer);
        }
    }

    void Update()
    {
        if (_isDestroyed) return;

        // Check game state - destroy silently if not simulating
        if (GameStateManager.Instance != null &&
            GameStateManager.Instance.CurrentState != GameState.Simulate)
        {
            DestroySilently();
            return;
        }

        _elapsedTime += Time.deltaTime;

        // Lifespan check - destroy silently when time runs out
        if (_elapsedTime >= _lifespan)
        {
            DestroySilently();
            return;
        }

        // Check if target is still valid
        if (!HasValidTarget())
        {
            // Try to find new target
            _retargetTimer += Time.deltaTime;
            if (_retargetTimer >= _retargetInterval)
            {
                _retargetTimer = 0f;
                FindNewTarget();
            }

            // REMOVED: Don't destroy if no target found
            // Keep flying in current direction until lifespan expires or hits something
            // if (!HasValidTarget())
            // {
            //     DestroySilently();
            //     return;
            // }
        }

        // Switch to homing after initial straight phase
        if (!_isHoming && _elapsedTime >= _initialStraightTime)
        {
            _isHoming = true;
        }

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

        // Movement towards current target (or straight if no target)
        if (HasValidTarget())
        {
            MoveTowardsTarget(_target.Transform.position);
        }
        else
        {
            // ADDED: No target - just keep flying straight
            transform.position += transform.right * _currentSpeed * Time.deltaTime;
        }

        // Keep Z at 0
        Vector3 pos = transform.position;
        pos.z = 0f;
        transform.position = pos;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_isDestroyed) return;

        if ((_targetLayers.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        ITroop hitTroop = other.GetComponentInParent<ITroop>() as ITroop;

        if (hitTroop != null && hitTroop.TeamIndex != _ownerTeamIndex && !hitTroop.IsDead)
        {
            DealDamageInRadius(transform.position);
            Explode();
        }
    }

    private bool HasValidTarget()
    {
        return _target != null && !_target.IsDead && _target.GameObject != null;
    }

    private void FindNewTarget()
    {
        if (TeamManager.Instance == null) return;

        ITroop nearestEnemy = null;
        float nearestDistance = _retargetRadius;

        // Search through all teams
        foreach (Team team in TeamManager.Instance.Teams)
        {
            if (team == null || team.TeamIndex == _ownerTeamIndex) continue;

            foreach (var kvp in team.Units)
            {
                if (kvp.Value == null) continue;

                foreach (ITroop candidate in kvp.Value)
                {
                    if (candidate == null || candidate.IsDead || candidate.GameObject == null)
                        continue;

                    float distance = Vector3.Distance(transform.position, candidate.Transform.position);

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = candidate;
                    }
                }
            }
        }

        if (nearestEnemy != null)
        {
            _target = nearestEnemy;
            Debug.Log($"[Rocket] Retargeted to {nearestEnemy.GameObject.name}");
        }
    }

    private void MoveTowardsTarget(Vector3 targetPosition)
    {
        if (_isHoming)
        {
            Vector3 directionToTarget = (targetPosition - transform.position).normalized;
            float targetAngle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
            float currentAngle = transform.eulerAngles.z;

            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, _currentTurnSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0, 0, newAngle);
        }

        transform.position += transform.right * _currentSpeed * Time.deltaTime;
    }

    private void DealDamageInRadius(Vector3 center)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, _explosionRadius);

        foreach (Collider2D hit in hits)
        {
            ITroop troop = hit.GetComponentInParent<ITroop>() as ITroop;

            if (troop != null && troop.TeamIndex != _ownerTeamIndex && !troop.IsDead)
            {
                troop.TakeDamage(_damage);
            }
        }
    }

    private void Explode()
    {
        if (_isDestroyed) return;
        _isDestroyed = true;

        if (_explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
            float scaleFactor = _explosionRadius / _vfxDesignRadius;
            effect.transform.localScale = Vector3.one * scaleFactor;
        }

        Destroy(gameObject);
    }

    private void DestroySilently()
    {
        if (_isDestroyed) return;
        _isDestroyed = true;
        Destroy(gameObject);
    }
}