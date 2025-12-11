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
                var field = typeof(Team).GetField("_teamIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(Teams[i], i);
                    Debug.Log($"[TeamManager] Set Team {i} index to {i}");
                }
            }
        }
    }

    // CHANGED: ITroop parameter instead of Troop
    private void OnUnitDied(ITroop troop)
    {
        if (troop == null)
        {
            Debug.LogWarning("[TeamManager] OnUnitDied called with null troop");
            return;
        }

        // Get team index from troop (works for both Unity and Atoms)
        int teamIndex = GetTeamIndexFromTroop(troop);

        if (teamIndex < 0)
        {
            Debug.LogError($"[TeamManager] Could not determine team index for {troop.GameObject?.name}");
            return;
        }

        Debug.Log($"[TeamManager] OnUnitDied called for {troop.GameObject?.name}, TeamIndex: {teamIndex}");

        Team team = GetTeamByIndex(teamIndex);
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
            Debug.LogError($"[TeamManager] Could not find team for index: {teamIndex}");
        }
    }

    // Helper to get team index from ITroop
    private int GetTeamIndexFromTroop(ITroop troop)
    {
        if (troop is Troop unityTroop)
        {
            return unityTroop.TeamIndex;
        }
        else if (troop is Troop_Atoms atomsTroop)
        {
            return atomsTroop.TeamIndex;
        }

        return -1;
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
