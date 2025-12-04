using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Rocket : MonoBehaviour
{
    private Troop _target;
    private int _damage;
    private System.Guid _teamID;
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

    private float _currentSpeed;
    private float _currentTurnSpeed;
    private float _elapsedTime;
    private bool _isHoming;
    private bool _isDestroyed;

    public void Initialize(
        Troop target, 
        int damage, 
        System.Guid teamID,
        float initialStraightTime,
        float initialSpeed,
        float maxSpeed,
        float acceleration,
        float initialTurnSpeed,
        float maxTurnSpeed,
        float turnAcceleration,
        float explosionRadius,
        float lifespan,
        GameObject explosionEffectPrefab)
    {
        _target = target;
        _damage = damage;
        _teamID = teamID;
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
        
        _currentSpeed = _initialSpeed;
        _currentTurnSpeed = _initialTurnSpeed;
        _elapsedTime = 0f;
        _isHoming = false;
        _isDestroyed = false;
    }

    void Update()
    {
        if (_isDestroyed) return;

        // Check if game is not in simulate state - destroy rocket
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Simulate)
        {
            DestroySilently();
            return;
        }

        _elapsedTime += Time.deltaTime;

        // Lifespan check
        if (_elapsedTime >= _lifespan)
        {
            DestroySilently();
            return;
        }

        if (!_isHoming && _elapsedTime >= _initialStraightTime)
        {
            _isHoming = true;
        }

        // Speed up
        if (_currentSpeed < _maxSpeed)
        {
            _currentSpeed += _acceleration * Time.deltaTime;
            _currentSpeed = Mathf.Min(_currentSpeed, _maxSpeed);
        }

        // Turn speed up
        if (_currentTurnSpeed < _maxTurnSpeed)
        {
            _currentTurnSpeed += _turnAcceleration * Time.deltaTime;
            _currentTurnSpeed = Mathf.Min(_currentTurnSpeed, _maxTurnSpeed);
        }

        // Retarget if current target is dead
        if (_isHoming)
        {
            if (_target == null || _target.CurrentHealth <= 0)
            {
                _target = FindNewTarget();
            }

            if (_target != null && _target.CurrentHealth > 0)
            {
                Vector3 direction = (_target.transform.position - transform.position).normalized;
                direction.z = 0f;
                
                float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                float currentAngle = transform.eulerAngles.z;
                float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, _currentTurnSpeed * Time.deltaTime);
                
                transform.rotation = Quaternion.Euler(0, 0, newAngle);
            }
        }

        // Move forward
        Vector3 forward = new Vector3(Mathf.Cos(transform.eulerAngles.z * Mathf.Deg2Rad), 
                                      Mathf.Sin(transform.eulerAngles.z * Mathf.Deg2Rad), 
                                      0f);
        transform.position += forward * _currentSpeed * Time.deltaTime;
        
        Vector3 pos = transform.position;
        pos.z = 0f;
        transform.position = pos;
    }

    private Troop FindNewTarget()
    {
        // Find all troops in range
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 20f);
        Troop closestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (Collider2D col in colliders)
        {
            Troop troop = col.GetComponentInParent<Troop>();
            if (troop != null && troop.TeamID != _teamID && troop.CurrentHealth > 0)
            {
                float distance = Vector3.Distance(transform.position, troop.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = troop;
                }
            }
        }

        if (closestTarget != null)
        {
            Debug.Log($"[Rocket] Retargeted to {closestTarget.name}");
        }

        return closestTarget;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_isDestroyed) return;

        Troop hitTroop = other.GetComponentInParent<Troop>();

        if (hitTroop != null && hitTroop.TeamID != _teamID)
        {
            Explode();
        }
    }

    void Explode()
    {
        if (_isDestroyed) return;
        _isDestroyed = true;

        if (_explosionEffectPrefab != null)
        {
            Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _explosionRadius);
        foreach (Collider2D col in colliders)
        {
            Troop troop = col.GetComponentInParent<Troop>();
            if (troop != null && troop.TeamID != _teamID)
            {
                troop.TakeDamage(_damage);
                Debug.Log($"[Rocket] Explosion hit {troop.name} for {_damage} damage");
            }
        }

        Destroy(gameObject);
    }

    void DestroySilently()
    {
        if (_isDestroyed) return;
        _isDestroyed = true;
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _explosionRadius);
    }
}
