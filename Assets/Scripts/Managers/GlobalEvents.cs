using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GlobalEvents
{
    public static event Action<Troop> UnitDied;

    public static void RaiseUnitDied(Troop troop)
    {
        UnitDied?.Invoke(troop);
    }
}
