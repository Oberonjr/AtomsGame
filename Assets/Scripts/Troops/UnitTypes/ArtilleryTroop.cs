using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

public class ArtilleryTroop : Troop
{
    [Header("Rocket References")]
    public GameObject RocketPrefab;
    public Transform LaunchPoint;

    [Header("Rocket Settings")]
    public float RocketInitialStraightTime = 0.5f;
    public float RocketInitialSpeed = 5f;
    public float RocketMaxSpeed = 20f;
    public float RocketAcceleration = 5f;
    public float RocketInitialTurnSpeed = 50f;
    public float RocketMaxTurnSpeed = 200f;
    public float RocketTurnAcceleration = 50f;
    public float RocketExplosionRadius = 3f;
    public float RocketLifespan = 10f;
    
    [Header("VFX Settings")]
    public GameObject RocketExplosionEffectPrefab;
    [Tooltip("The radius that your VFX was designed for (reference size). The VFX will be automatically scaled to match the explosion radius.")]
    public float VFXDesignRadius = 0.1f; // Adjust this to your VFX's natural size

    protected override void OnStart()
    {
    }

    public override void Attack()
    {
        if (Target == null || Target.IsDead || RocketPrefab == null)
            return;

        if (_animController != null)
        {
            _animController.PlayAttackAnimation();
        }

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
        Rocket rocket = rocketObj.GetComponent<Rocket>();

        if (rocket != null)
        {
            rocket.Initialize(
                Target, 
                TroopStats.Damage, 
                TeamID,
                RocketInitialStraightTime,
                RocketInitialSpeed,
                RocketMaxSpeed,
                RocketAcceleration,
                RocketInitialTurnSpeed,
                RocketMaxTurnSpeed,
                RocketTurnAcceleration,
                RocketExplosionRadius,
                RocketLifespan,
                RocketExplosionEffectPrefab,
                VFXDesignRadius
            );
            Debug.Log($"[ArtilleryTroop] Fired rocket at {Target.name}");
        }
    }
}