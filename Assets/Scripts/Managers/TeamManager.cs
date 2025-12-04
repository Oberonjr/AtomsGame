using System.Collections.Generic;
using UnityEngine;

public class TeamManager : MonoBehaviour
{
    private static TeamManager _instance;
    public static TeamManager Instance => _instance;

    public List<Team> Teams = new List<Team>();

    public delegate void TeamDefeatedHandler(Team team);
    public static event TeamDefeatedHandler TeamDefeated;

    void OnEnable()
    {
        foreach (Team team in Teams)
        {
            GlobalEvents.UnitDied += OnUnitDied;
        }
    }

    void OnDisable()
    {
        foreach (Team team in Teams)
        {
            GlobalEvents.UnitDied -= OnUnitDied;
        }
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Debug.LogWarning("More than one instance of TeamManager! Destroying: " + gameObject.name);
            Destroy(gameObject);
        }
    }

    private void OnUnitDied(Troop troop)
    {
        foreach (Team team in Teams)
        {
            if (team.ID == troop.TeamID)
            {
                team.OnUnitDied(troop);
                if (team.TotalUnits() == 0)
                {
                    TeamDefeated?.Invoke(team);
                }
                break;
            }
        }
    }
}
