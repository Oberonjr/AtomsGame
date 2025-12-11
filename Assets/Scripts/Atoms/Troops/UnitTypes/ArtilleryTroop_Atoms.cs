using System.Collections;
using UnityEngine;
using UnityAtoms.BaseAtoms;

/// <summary>
/// Atoms artillery troop - fires homing rockets
/// </summary>
public class ArtilleryTroop_Atoms : Troop_Atoms
{
    [Header("Artillery Settings - Atoms Variables")]
    [SerializeField] private FloatVariable _rocketInitialStraightTime;
    [SerializeField] private FloatVariable _rocketInitialSpeed;
    [SerializeField] private FloatVariable _rocketMaxSpeed;
    [SerializeField] private FloatVariable _rocketAcceleration;
    [SerializeField] private FloatVariable _rocketInitialTurnSpeed;
    [SerializeField] private FloatVariable _rocketMaxTurnSpeed;
    [SerializeField] private FloatVariable _rocketTurnAcceleration;
    [SerializeField] private FloatVariable _rocketExplosionRadius;
    [SerializeField] private FloatVariable _rocketLifespan;
    [SerializeField] private FloatVariable _vfxDesignRadius;
    
    [Header("References")]
    public Transform LaunchPoint;
    public GameObject RocketPrefab;
    public GameObject RocketExplosionEffectPrefab;
    
    // Cached values
    private float _cachedInitialStraightTime;
    private float _cachedInitialSpeed;
    private float _cachedMaxSpeed;
    private float _cachedAcceleration;
    private float _cachedInitialTurnSpeed;
    private float _cachedMaxTurnSpeed;
    private float _cachedTurnAcceleration;
    private float _cachedExplosionRadius;
    private float _cachedLifespan;
    private float _cachedVFXRadius;

    protected override void OnStart()
    {
        base.OnStart();
        
        // Cache all Atoms variables
        _cachedInitialStraightTime = _rocketInitialStraightTime?.Value ?? 0.5f;
        _cachedInitialSpeed = _rocketInitialSpeed?.Value ?? 5f;
        _cachedMaxSpeed = _rocketMaxSpeed?.Value ?? 15f;
        _cachedAcceleration = _rocketAcceleration?.Value ?? 10f;
        _cachedInitialTurnSpeed = _rocketInitialTurnSpeed?.Value ?? 90f;
        _cachedMaxTurnSpeed = _rocketMaxTurnSpeed?.Value ?? 360f;
        _cachedTurnAcceleration = _rocketTurnAcceleration?.Value ?? 180f;
        _cachedExplosionRadius = _rocketExplosionRadius?.Value ?? 3f;
        _cachedLifespan = _rocketLifespan?.Value ?? 5f;
        _cachedVFXRadius = _vfxDesignRadius?.Value ?? 1f;
        
        // Subscribe to changes
        _rocketInitialStraightTime?.Changed.Register(v => _cachedInitialStraightTime = v);
        _rocketInitialSpeed?.Changed.Register(v => _cachedInitialSpeed = v);
        _rocketMaxSpeed?.Changed.Register(v => _cachedMaxSpeed = v);
        _rocketAcceleration?.Changed.Register(v => _cachedAcceleration = v);
        _rocketInitialTurnSpeed?.Changed.Register(v => _cachedInitialTurnSpeed = v);
        _rocketMaxTurnSpeed?.Changed.Register(v => _cachedMaxTurnSpeed = v);
        _rocketTurnAcceleration?.Changed.Register(v => _cachedTurnAcceleration = v);
        _rocketExplosionRadius?.Changed.Register(v => _cachedExplosionRadius = v);
        _rocketLifespan?.Changed.Register(v => _cachedLifespan = v);
        _vfxDesignRadius?.Changed.Register(v => _cachedVFXRadius = v);
    }
    
    void OnDestroy()
    {
        // Unsubscribe
        _rocketInitialStraightTime?.Changed.Unregister(v => _cachedInitialStraightTime = v);
        _rocketInitialSpeed?.Changed.Unregister(v => _cachedInitialSpeed = v);
        _rocketMaxSpeed?.Changed.Unregister(v => _cachedMaxSpeed = v);
        _rocketAcceleration?.Changed.Unregister(v => _cachedAcceleration = v);
        _rocketInitialTurnSpeed?.Changed.Unregister(v => _cachedInitialTurnSpeed = v);
        _rocketMaxTurnSpeed?.Changed.Unregister(v => _cachedMaxTurnSpeed = v);
        _rocketTurnAcceleration?.Changed.Unregister(v => _cachedTurnAcceleration = v);
        _rocketExplosionRadius?.Changed.Unregister(v => _cachedExplosionRadius = v);
        _rocketLifespan?.Changed.Unregister(v => _cachedLifespan = v);
        _vfxDesignRadius?.Changed.Unregister(v => _cachedVFXRadius = v);
    }

    public override void Attack()
    {
        if (IsDead) return;
        if (Target == null || Target.IsDead) return;

        // Stop moving
        if (Agent != null && Agent.enabled && Agent.isOnNavMesh)
        {
            Agent.isStopped = true;
        }

        if(_animController != null)
        {
            // Animation controller handles attack animation
            _animController.PlayAttackAnimation();
        }

        // Spawn rocket
        SpawnRocket();
    }

    private void SpawnRocket()
    {
        Vector3 spawnPosition = LaunchPoint != null ? LaunchPoint.position : transform.position;
        
        Quaternion spawnRotation = Quaternion.identity;
        if (Target != null)
        {
            Vector3 direction = (Target.transform.position - spawnPosition).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            spawnRotation = Quaternion.Euler(0, 0, angle);
        }

        GameObject rocketObj = Instantiate(RocketPrefab, spawnPosition, spawnRotation);
        Rocket rocket = rocketObj.GetComponent<Rocket>(); // CHANGED: Use Rocket instead of Rocket_Atoms

        if (rocket != null)
        {
            // CHANGED: Pass ITroop instead of Troop_Atoms
            rocket.Initialize(
                Target as ITroop, // Cast to ITroop
                GetDamage(),
                TeamIndex,
                _cachedInitialStraightTime,
                _cachedInitialSpeed,
                _cachedMaxSpeed,
                _cachedAcceleration,
                _cachedInitialTurnSpeed,
                _cachedMaxTurnSpeed,
                _cachedTurnAcceleration,
                _cachedExplosionRadius,
                _cachedLifespan,
                RocketExplosionEffectPrefab,
                _cachedVFXRadius
            );
        }
    }
}