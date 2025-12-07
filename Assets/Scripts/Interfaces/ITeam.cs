using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Interface for team management (Unity and Atoms)
/// </summary>
public interface ITeam
{
    Guid ID { get; } // Deprecated but kept for compatibility
    int TeamIndex { get; } // NEW: Primary identifier
    Color TeamColor { get; set; }
    TeamArea Area { get; }

    int TotalUnits();
    void RegisterUnit(ITroop troop);
    void OnUnitDied(ITroop troop);
    void ClearUnits();

    IEnumerable<ITroop> GetAllUnits();
}