using System;
using System.Collections.Generic;
using UnityEngine;
using AYellowpaper.SerializedCollections;

[System.Serializable]
public class Team : ITeam, IEquatable<Team>
{
    [SerializeField] private int _teamIndex;
    [SerializeField] private Color _teamColor = Color.white;
    [SerializeField] private TeamArea _area;

    // CHANGED: Initialize immediately to prevent null
    public Dictionary<TroopType, List<ITroop>> Units = new Dictionary<TroopType, List<ITroop>>();

    // Events
    public event Action<Color> OnTeamColorChanged;

    // Properties
    public int TeamIndex => _teamIndex;
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

    // IMPLEMENT ITeam INTERFACE (no ID property)
    Color ITeam.TeamColor { get => _teamColor; set => TeamColor = value; }
    TeamArea ITeam.Area => _area;
    int ITeam.TeamIndex => _teamIndex;

    int ITeam.TotalUnits()
    {
        return TotalUnits();
    }

    void ITeam.RegisterUnit(ITroop troop)
    {
        RegisterUnit(troop);
    }

    void ITeam.OnUnitDied(ITroop troop)
    {
        OnUnitDied(troop);
    }

    void ITeam.ClearUnits()
    {
        ClearUnits();
    }

    IEnumerable<ITroop> ITeam.GetAllUnits()
    {
        foreach (var kvp in Units)
        {
            foreach (ITroop troop in kvp.Value)
            {
                yield return troop;
            }
        }
    }

    // Constructor for proper initialization
    public Team(int teamIndex)
    {
        _teamIndex = teamIndex;
        // ADDED: Ensure Units is initialized
        Units = new Dictionary<TroopType, List<ITroop>>();
    }
    
    // ADDED: Parameterless constructor for serialization
    public Team()
    {
        Units = new Dictionary<TroopType, List<ITroop>>();
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

    // UNIFIED RegisterUnit - works for both Unity and Atoms
    public void RegisterUnit(ITroop troop)
    {
        if (troop == null)
        {
            Debug.LogWarning("[Team] Attempted to register null ITroop");
            return;
        }

        // ADDED: Check GameObject before accessing it
        if (troop.GameObject == null)
        {
            Debug.LogWarning("[Team] Attempted to register troop with null GameObject");
            return;
        }

        // Get TroopType from the troop
        TroopType type = GetTroopType(troop);

        if (!Units.ContainsKey(type))
        {
            Units[type] = new List<ITroop>();
            Debug.Log($"[Team {_teamIndex}] Created new list for {type}");
        }

        if (!Units[type].Contains(troop))
        {
            Units[type].Add(troop);
            // FIXED: Safe null check
            string troopName = troop.GameObject != null ? troop.GameObject.name : "Unknown";
            Debug.Log($"[Team {_teamIndex}] Registered {type} troop ({troopName}). Total: {TotalUnits()}");
        }
        else
        {
            string troopName = troop.GameObject != null ? troop.GameObject.name : "Unknown";
            Debug.LogWarning($"[Team {_teamIndex}] Troop {troopName} already registered");
        }
    }

    // UNIFIED OnUnitDied - works for both Unity and Atoms
    public void OnUnitDied(ITroop troop)
    {
        if (troop == null)
        {
            Debug.LogWarning("[Team] OnUnitDied called with null ITroop");
            return;
        }

        TroopType type = GetTroopType(troop);

        if (Units.ContainsKey(type))
        {
            bool removed = Units[type].Remove(troop);
            Debug.Log($"[Team {_teamIndex}] Unit died ({troop.GameObject?.name}). Removed: {removed}. Remaining: {TotalUnits()}");

            // Clean up empty lists
            if (Units[type].Count == 0)
            {
                Units.Remove(type);
                Debug.Log($"[Team {_teamIndex}] Removed empty list for {type}");
            }
        }
        else
        {
            Debug.LogWarning($"[Team {_teamIndex}] Units dictionary does not contain key: {type}");
        }
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
        Debug.Log($"[Team {_teamIndex}] All units cleared");
    }

    // Helper method to get TroopType from ITroop
    private TroopType GetTroopType(ITroop troop)
    {
        // Try Unity Troop first
        if (troop is Troop unityTroop && unityTroop.TroopStats != null)
        {
            return unityTroop.TroopStats.TroopType;
        }
        
        // Try Atoms Troop
        if (troop is Troop_Atoms atomsTroop)
        {
            return atomsTroop.Stats.TroopType;
        }

        // Fallback
        Debug.LogWarning($"[Team] Could not determine TroopType for {troop.GameObject?.name}, defaulting to MELEE");
        return TroopType.MELEE;
    }

    // IEquatable implementation (KEPT)
    public bool Equals(Team other)
    {
        if (other == null) return false;
        return _teamIndex == other._teamIndex;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as Team);
    }

    public override int GetHashCode()
    {
        return _teamIndex.GetHashCode();
    }
}