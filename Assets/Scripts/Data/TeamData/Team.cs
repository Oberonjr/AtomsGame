using System;
using System.Collections.Generic;
using UnityEngine;
using AYellowpaper.SerializedCollections;

[System.Serializable]
public class Team : ITeam
{
    [SerializeField] private int _teamIndex; // NEW: Simple integer index
    [SerializeField] private Color _teamColor = Color.white;
    [SerializeField] private TeamArea _area;

    public SerializedDictionary<TroopStats, List<Troop>> Units = new SerializedDictionary<TroopStats, List<Troop>>();

    // Events
    public event Action<Color> OnTeamColorChanged;

    // Properties
    public int TeamIndex => _teamIndex; // NEW: Expose team index
    public Guid ID => Guid.Empty; // DEPRECATED: Keep for interface compatibility, but unused
    public Color TeamColor
    {
        get => _teamColor;
        set
        {
            if (_teamColor != value)
            {
                _teamColor = value;
                OnTeamColorChanged?.Invoke(_teamColor);
            }
        }
    }
    public TeamArea Area => _area;

    // IMPLEMENT ITeam INTERFACE
    Guid ITeam.ID => Guid.Empty; // Deprecated
    Color ITeam.TeamColor 
    { 
        get => _teamColor; 
        set => TeamColor = value; 
    }
    TeamArea ITeam.Area => _area;
    int ITeam.TeamIndex => _teamIndex; // ADD THIS

    int ITeam.TotalUnits()
    {
        return TotalUnits();
    }

    void ITeam.RegisterUnit(ITroop troop)
    {
        if (troop is Troop troopMono)
        {
            RegisterUnit(troopMono);
        }
    }

    void ITeam.OnUnitDied(ITroop troop)
    {
        if (troop is Troop troopMono)
        {
            OnUnitDied(troopMono);
        }
    }

    void ITeam.ClearUnits()
    {
        ClearUnits();
    }

    IEnumerable<ITroop> ITeam.GetAllUnits()
    {
        foreach (var kvp in Units)
        {
            foreach (Troop troop in kvp.Value)
            {
                yield return troop;
            }
        }
    }

    // Constructor for proper initialization
    public Team(int teamIndex)
    {
        _teamIndex = teamIndex;
    }

    // EXISTING METHODS
    public void Initialize()
    {
        if (_area != null)
        {
            _area.SubscribeToTeam(this);
            _area.SetTeamColor(_teamColor);
        }
    }

    public void RegisterUnit(Troop troop)
    {
        if (troop == null || troop.TroopStats == null)
        {
            Debug.LogWarning("[Team] Attempted to register null troop or troop without stats");
            return;
        }

        if (!Units.ContainsKey(troop.TroopStats))
        {
            Units[troop.TroopStats] = new List<Troop>();
            Debug.Log($"[Team] Created new list for {troop.TroopStats.name}");
        }

        if (!Units[troop.TroopStats].Contains(troop))
        {
            Units[troop.TroopStats].Add(troop);
            Debug.Log($"[Team] Registered {troop.name} ({troop.TroopStats.name}). Total: {TotalUnits()}");
        }
        else
        {
            Debug.LogWarning($"[Team] Troop {troop.name} already registered");
        }
    }

    public void OnUnitDied(Troop troop)
    {
        if (troop == null || troop.TroopStats == null)
        {
            Debug.LogWarning("[Team] OnUnitDied called with null troop or stats");
            return;
        }

        Debug.Log($"[Team] OnUnitDied called for {troop.name} ({troop.TroopStats.name})");

        if (Units.ContainsKey(troop.TroopStats))
        {
            bool removed = Units[troop.TroopStats].Remove(troop);
            Debug.Log($"[Team] Removed from list: {removed}. Remaining in this stats category: {Units[troop.TroopStats].Count}");
            
            // Clean up empty lists
            if (Units[troop.TroopStats].Count == 0)
            {
                Units.Remove(troop.TroopStats);
                Debug.Log($"[Team] Removed empty list for {troop.TroopStats.name}");
            }
        }
        else
        {
            Debug.LogWarning($"[Team] Units dictionary does not contain key: {troop.TroopStats.name}");
        }
        
        // NEW: Log final count
        int total = TotalUnits();
        Debug.Log($"[Team] Team {TeamIndex} now has {total} total units");
    }

    public int TotalUnits()
    {
        int total = 0;
        
        if (Units == null) return 0;
        
        foreach (var kvp in Units)
        {
            if (kvp.Value != null)
            {
                total += kvp.Value.Count;
            }
        }
        return total;
    }

    public void ClearUnits()
    {
        Units.Clear();
        Debug.Log("[Team] All units cleared");
    }
}