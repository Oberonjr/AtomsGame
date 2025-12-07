using System.Collections.Generic;
using UnityEngine;

public class TeamManager : MonoBehaviour
{
    private static TeamManager _instance;
    public static TeamManager Instance => _instance;

    public List<Team> Teams = new List<Team>();

    public delegate void TeamDefeatedHandler(Team team);
    public static event TeamDefeatedHandler TeamDefeated;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            
            // ENSURE TEAM INDICES ARE SET
            ValidateTeamIndices();
        }
        else if (_instance != this)
        {
            Debug.LogWarning("More than one instance of TeamManager! Destroying: " + gameObject.name);
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        GlobalEvents.UnitDied += OnUnitDied;
    }

    void OnDisable()
    {
        GlobalEvents.UnitDied -= OnUnitDied;
    }

    private void ValidateTeamIndices()
    {
        for (int i = 0; i < Teams.Count; i++)
        {
            if (Teams[i] != null)
            {
                // Use reflection to set team index if not already set
                var field = typeof(Team).GetField("_teamIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(Teams[i], i);
                    Debug.Log($"[TeamManager] Set Team {i} index to {i}");
                }
            }
        }
    }

    private void OnUnitDied(Troop troop)
    {
        if (troop == null)
        {
            Debug.LogWarning("[TeamManager] OnUnitDied called with null troop");
            return;
        }

        Debug.Log($"[TeamManager] OnUnitDied called for {troop.name}, TeamIndex: {troop.TeamIndex}");

        // Find team by index instead of GUID
        Team team = GetTeamByIndex(troop.TeamIndex);
        if (team != null)
        {
            Debug.Log($"[TeamManager] Found team {team.TeamIndex}, removing unit");
            team.OnUnitDied(troop);
            
            int remainingUnits = team.TotalUnits();
            Debug.Log($"[TeamManager] Team {team.TeamIndex} has {remainingUnits} units remaining");
            
            if (remainingUnits == 0)
            {
                Debug.Log($"[TeamManager] Team {team.TeamIndex} defeated! Raising TeamDefeated event");
                TeamDefeated?.Invoke(team);
            }
        }
        else
        {
            Debug.LogError($"[TeamManager] Could not find team for index: {troop.TeamIndex}");
        }
    }

    public void CheckTeamStatus(Team team)
    {
        if (team == null) return;
        
        if (team.TotalUnits() <= 0)
        {
            Debug.Log($"[TeamManager] Team {team.TeamIndex} has been defeated!");
            TeamDefeated?.Invoke(team);
        }
    }

    // NEW: Helper methods
    public Team GetTeamByIndex(int index)
    {
        if (index >= 0 && index < Teams.Count)
        {
            return Teams[index];
        }
        return null;
    }

    public int GetTeamIndex(Team team)
    {
        return Teams.IndexOf(team);
    }
}
