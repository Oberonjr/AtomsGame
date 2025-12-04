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
    public GameObject RocketExplosionEffectPrefab;

    private TroopAnimationController _animController;

    protected override void OnStart()
    {
        _animController = GetComponent<TroopAnimationController>();
    }

    public override void Attack()
    {
        if (Target != null && Target.CurrentHealth > 0 && RocketPrefab != null)
        {
            // Play attack animation FIRST
            if (_animController != null)
            {
                _animController.PlayAttackAnimation();
            }

            // Spawn rocket after animation delay (handled by Invoke in animation controller)
            // Or spawn immediately:
            SpawnRocket();
        }
    }

    private void SpawnRocket()
    {
        Vector3 spawnPosition = LaunchPoint != null ? LaunchPoint.position : transform.position;
        
        // Calculate rotation to face target in 2D
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
            // Pass all settings to rocket
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
                RocketExplosionEffectPrefab
            );
            Debug.Log($"[ArtilleryTroop] Fired rocket at {Target.name}");
        }
    }
}