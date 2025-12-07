using System;
using UnityEngine;

public static class GlobalEvents
{
    public static event Action<Troop> UnitDied;

    public static void RaiseUnitDied(Troop troop)
    {
        if (troop == null)
        {
            Debug.LogError("[GlobalEvents] RaiseUnitDied called with null troop!");
            return;
        }
        
        Debug.Log($"[GlobalEvents] Raising UnitDied event for {troop.name}. Subscribers: {UnitDied?.GetInvocationList().Length ?? 0}");
        UnitDied?.Invoke(troop);
    }
}
