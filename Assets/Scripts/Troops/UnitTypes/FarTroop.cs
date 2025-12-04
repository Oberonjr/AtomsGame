using System.Collections;
using System.Collections.Generic;
using UnityEngine;  

public class FarTroop : Troop
{
    public Transform FirePoint; // Assign in inspector - point from where raycast originates
    public GameObject HitEffectPrefab; // Optional: visual effect on hit
    public LineRenderer LineRenderer; // Add in Inspector
    public float LineDuration = 0.1f;
    
    [Header("Retreat Settings")]
    public float CloseRangeDistance = 7f;
    public float RetreatSpeed = 3f;

    private float _lineTimer;

    // Remove the Start() method entirely - use Awake or override properly
    protected override void OnStart()
    {
        // Called from base Troop.Start() after FSM initialization
        
        // Setup LineRenderer if not assigned
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

    // Override Update to add line timer logic (call base!)
    protected override void OnUpdate()
    {
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
        if (Target == null || Target.CurrentHealth <= 0)
            return;

        float distance = Vector3.Distance(transform.position, Target.transform.position);
        bool isClose = distance < CloseRangeDistance;
        
        // Play attack animation
        if (_animController != null)
        {
            _animController.PlayAttackAnimation();
        }

        // If too close, retreat while shooting
        if (isClose)
        {
            if (Agent != null && Agent.isOnNavMesh)
            {
                Vector3 retreatDirection = (transform.position - Target.transform.position).normalized;
                Vector3 retreatTarget = transform.position + retreatDirection * RetreatSpeed;
                retreatTarget.z = 0f;
                
                if (UnityEngine.AI.NavMesh.SamplePosition(retreatTarget, out UnityEngine.AI.NavMeshHit hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    Agent.SetDestination(hit.position);
                    Agent.isStopped = false;
                }
            }
        }
        else
        {
            // Stop moving when at safe distance
            if (Agent != null && Agent.isOnNavMesh)
            {
                Agent.isStopped = true;
            }
        }

        // Always shoot regardless of distance
        PerformRaycastAttack();
    }

    private void PerformRaycastAttack()
    {
        if (Target == null) return;

        Vector3 firePosition = FirePoint != null ? FirePoint.position : transform.position;
        Vector3 direction = (Target.transform.position - firePosition).normalized;

        // Draw line
        if (LineRenderer != null)
        {
            LineRenderer.SetPosition(0, firePosition);
            LineRenderer.SetPosition(1, Target.transform.position);
            LineRenderer.enabled = true;
            _lineTimer = LineDuration;
        }

        // Perform raycast
        RaycastHit2D rayHit = Physics2D.Raycast(firePosition, direction, TroopStats.AttackRange);
        
        if (rayHit.collider != null)
        {
            Troop hitTroop = rayHit.collider.GetComponentInParent<Troop>();
            if (hitTroop != null && hitTroop.TeamID != TeamID)
            {
                hitTroop.TakeDamage(TroopStats.Damage);
                Debug.Log($"[FarTroop] Hit {hitTroop.name} for {TroopStats.Damage} damage");

                if (HitEffectPrefab != null)
                {
                    Instantiate(HitEffectPrefab, rayHit.point, Quaternion.identity);
                }
            }
        }
    }
}
