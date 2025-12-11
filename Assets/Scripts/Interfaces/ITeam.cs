using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Interface for team management (Unity and Atoms)
/// </summary>
public interface ITeam
{
    int TeamIndex { get; } // Primary identifier
    Color TeamColor { get; set; }
    TeamArea Area { get; }

    int TotalUnits();
    void RegisterUnit(ITroop troop);
    void OnUnitDied(ITroop troop);
    void ClearUnits();

    IEnumerable<ITroop> GetAllUnits();
}