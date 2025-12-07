using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CloseTroop : Troop
{
    [Header("Melee Settings")]
    [Tooltip("Distance at which the agent will stop moving towards target")]
    public float StoppingDistance = 0.8f;

    [Tooltip("Smaller radius for less collision with other melee units")]
    public float AgentRadius = 0.4f;

    [Tooltip("How much to prefer current target over switching (reduces retargeting)")]
    public float TargetStickyness = 0.9f;

    protected override void OnStart()
    {
        // Melee-specific NavMesh configuration
        if (Agent != null)
        {
            // Stop slightly before reaching attack range to prevent bunching
            Agent.stoppingDistance = TroopStats.AttackRange * StoppingDistance;
            Agent.autoBraking = true; // Smooth deceleration
            Agent.radius = AgentRadius; // Smaller collision radius

            // Higher quality avoidance for melee units
            Agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            Agent.avoidancePriority = UnityEngine.Random.Range(40, 60); // Randomize to prevent sync
        }

        Debug.Log($"[MeleeTroop] {name} configured with stopping distance: {Agent.stoppingDistance}");
    }

    protected override void OnUpdate()
    {
        // Melee-specific update logic
        if (Agent != null && Agent.isOnNavMesh)
        {
            // Force stop if in attack range and attacking
            if (Target != null && IsInRange(Target) && FSM?.CurrentState is AttackState)
            {
                Agent.isStopped = true;
                Agent.velocity = Vector3.zero; // Clear any residual momentum
            }
        }
    }

    public override void Attack()
    {
        // IMMEDIATE DEATH CHECK
        if (IsDead) return;
        
        if (Target == null || Target.IsDead)
            return;

        // Ensure we're completely stopped before attacking
        if (Agent != null && Agent.enabled && Agent.isOnNavMesh)
        {
            Agent.isStopped = true;
            Agent.velocity = Vector3.zero;
        }

        // Play animation and deal damage
        if (_animController != null)
        {
            _animController.PlayAttackAnimation();
        }

        Target.TakeDamage(TroopStats.Damage);
    }

    // Override IsInRange with a buffer zone to prevent state thrashing
    public new bool IsInRange(Troop target)
    {
        if (target == null) return false;

        float distance = Vector3.Distance(transform.position, target.transform.position);

        // Add hysteresis: different range for entering vs exiting attack state
        if (FSM?.CurrentState is AttackState)
        {
            // Once attacking, need to go further away to switch back to move
            return distance <= TroopStats.AttackRange + 0.3f;
        }
        else
        {
            // When moving, need to get closer to start attacking
            return distance <= TroopStats.AttackRange;
        }
    }
}
