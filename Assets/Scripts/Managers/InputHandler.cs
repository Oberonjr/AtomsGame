using UnityEngine;
using System;

/// <summary>
/// Handles user input during prep phase
/// </summary>
public class InputHandler : MonoBehaviour
{
    // MAKE THESE PUBLIC FOR INSPECTOR
    [Header("Raycast Settings")]
    public LayerMask GroundLayer;
    public LayerMask UILayer;
    
    public event Action<Vector3, TeamArea> OnPlaceUnitRequested;
    public event Action<ITroop> OnRemoveUnitRequested;
    
    private bool _isEnabled = false;
    
    void Awake()
    {
        // Set defaults if not configured
        if (GroundLayer == 0)
        {
            GroundLayer = LayerMask.GetMask("Ground");
            Debug.LogWarning("[InputHandler] GroundLayer not set, using 'Ground' layer");
        }
        if (UILayer == 0)
        {
            UILayer = LayerMask.GetMask("UI");
            Debug.LogWarning("[InputHandler] UILayer not set, using 'UI' layer");
        }
    }
    
    public void Enable()
    {
        _isEnabled = true;
    }
    
    public void Disable()
    {
        _isEnabled = false;
    }
    
    void Update()
    {
        if (!_isEnabled) return;

        // Check if UI panels are blocking input
        if (GameUIManager.Instance != null && GameUIManager.Instance.ArePanelsActive())
        {
            return;
        }

        // Left click - place unit
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI()) return;

            HandlePlaceUnit();
        }
        // Right click - remove unit
        else if (Input.GetMouseButtonDown(1))
        {
            if (IsPointerOverUI()) return;

            HandleRemoveUnit();
        }
    }

    private bool IsPointerOverUI()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, UILayer))
        {
            return true;
        }

        return false;
    }

    private void HandlePlaceUnit()
    {
        if (Camera.main == null)
        {
            Debug.LogError("[InputHandler] Camera.main is null!");
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, GroundLayer))
        {
            TeamArea teamArea = FindTeamAreaAtPosition(hit.point);
            if (teamArea != null)
            {
                OnPlaceUnitRequested?.Invoke(hit.point, teamArea);
            }
        }
    }

    private void HandleRemoveUnit()
    {
        if (Camera.main == null) return;
        
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity);
        
        if (hit.collider != null)
        {
            // Try both types
            ITroop troop = hit.collider.GetComponentInParent<Troop>() as ITroop;
            
            if (troop == null)
            {
                troop = hit.collider.GetComponentInParent<Troop_Atoms>() as ITroop;
            }
            
            if (troop != null)
            {
                OnRemoveUnitRequested?.Invoke(troop);
            }
            else
            {
                Debug.LogWarning($"[InputHandler] No ITroop component found on {hit.collider.name}");
            }
        }
    }

    private TeamArea FindTeamAreaAtPosition(Vector3 position)
    {
        if (TeamManager.Instance == null) return null;

        foreach (Team team in TeamManager.Instance.Teams)
        {
            if (team?.Area != null)
            {
                float distance = Vector3.Distance(team.Area.transform.position, position);
                if (distance <= team.Area.Radius)
                {
                    return team.Area;
                }
            }
        }
        return null;
    }
}