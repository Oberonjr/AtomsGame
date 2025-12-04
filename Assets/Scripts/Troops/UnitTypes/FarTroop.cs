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

    private TroopAnimationController _animController;
    private float _lineTimer;

    // Remove the Start() method entirely - use Awake or override properly
    protected override void OnStart()
    {
        // Called from base Troop.Start() after FSM initialization
        _animController = GetComponent<TroopAnimationController>();
        
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

    void Update()
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
        if (Target != null && Target.CurrentHealth > 0)
        {
            // Play attack animation FIRST
            if (_animController != null)
            {
                _animController.PlayAttackAnimation();
            }

            float distance = Vector3.Distance(transform.position, Target.transform.position);
            bool isClose = distance < CloseRangeDistance;
            
            // Retreat if too close
            if (isClose && Agent != null && Agent.isOnNavMesh)
            {
                Vector3 retreatDirection = (transform.position - Target.transform.position).normalized;
                Vector3 retreatPosition = transform.position + retreatDirection * RetreatSpeed * Time.deltaTime;
                retreatPosition.z = 0f;
                
                // Only retreat if still on NavMesh
                if (UnityEngine.AI.NavMesh.SamplePosition(retreatPosition, out UnityEngine.AI.NavMeshHit hit, 1f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    Agent.SetDestination(hit.position);
                }
            }
            
            // Perform raycast attack
            Vector3 firePosition = FirePoint != null ? FirePoint.position : transform.position;
            Vector3 direction = (Target.transform.position - firePosition).normalized;

            // Draw line in playmode
            if (LineRenderer != null)
            {
                LineRenderer.SetPosition(0, firePosition);
                LineRenderer.SetPosition(1, Target.transform.position);
                LineRenderer.enabled = true;
                _lineTimer = LineDuration;
            }

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
}
