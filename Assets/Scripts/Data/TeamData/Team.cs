using System;
using System.Collections.Generic;
using UnityEngine;
using AYellowpaper.SerializedCollections;

[Serializable]
public class Team
{
    public Guid ID = Guid.NewGuid();

    [SerializedDictionary("Troop Stats", "Troop List")]
    public SerializedDictionary<TroopStats, List<Troop>> Units = new();

    [SerializeField] private TeamArea _area;
    [SerializeField] private Color _teamColor = Color.white;

    public event Action<Color> OnTeamColorChanged;
    public event Action<TeamArea> OnAreaChanged;

    public TeamArea Area
    {
        get => _area;
        set
        {
            if (_area != value)
            {
                // Unsubscribe from old area
                if (_area != null)
                {
                    _area.UnsubscribeFromTeam(this);
                }

                _area = value;

                // Subscribe to new area
                if (_area != null)
                {
                    _area.SubscribeToTeam(this);
                    _area.SetTeamColor(_teamColor);
                }

                OnAreaChanged?.Invoke(_area);
            }
        }
    }

    public Color TeamColor
    {
        get => _teamColor;
        set
        {
            if (_teamColor != value)
            {
                _teamColor = value;

                // Update the area's color if it exists
                if (_area != null)
                {
                    _area.SetTeamColor(_teamColor);
                }

                OnTeamColorChanged?.Invoke(_teamColor);
            }
        }
    }

    public void RegisterUnit(Troop troop)
    {
        if (troop == null || troop.TroopStats == null) return;

        if (!Units.ContainsKey(troop.TroopStats))
            Units[troop.TroopStats] = new List<Troop>();

        if (!Units[troop.TroopStats].Contains(troop))
            Units[troop.TroopStats].Add(troop);
    }

    public void OnUnitDied(Troop troop)
    {
        if (troop == null || troop.TeamID != ID) return;

        if (troop.TroopStats != null && Units.ContainsKey(troop.TroopStats))
        {
            Units[troop.TroopStats].Remove(troop);
            if (Units[troop.TroopStats].Count == 0)
                Units.Remove(troop.TroopStats);
        }
    }

    public int TotalUnits()
    {
        int count = 0;
        foreach (var kvp in Units)
        {
            if (kvp.Value != null)
                count += kvp.Value.Count;
        }
        return count;
    }

    public void ClearUnits()
    {
        Units.Clear();
    }
}