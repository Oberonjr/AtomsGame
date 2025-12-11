using System;
using UnityEngine;

public static class GlobalEvents
{
    // CHANGED: Use ITroop instead of Troop
    public static event Action<ITroop> UnitDied;

    public static void RaiseUnitDied(ITroop troop)
    {
        if (troop == null)
        {
            Debug.LogError("[GlobalEvents] RaiseUnitDied called with null troop!");
            return;
        }
        
        string troopName = troop.GameObject != null ? troop.GameObject.name : "Unknown";
        Debug.Log($"[GlobalEvents] Raising UnitDied event for {troopName}. Subscribers: {UnitDied?.GetInvocationList().Length ?? 0}");
        UnitDied?.Invoke(troop);
    }
}
