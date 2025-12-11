using System;
using UnityEngine;

[System.Serializable]
public class TroopSnapshot
{
    public TroopType TroopType; // CHANGED: Use enum instead of TroopStats reference
    public Vector3 Position;
    public Quaternion Rotation;
    public int TeamIndex;
    public int CurrentHealth;

    public TroopSnapshot(ITroop troop)
    {
        // Get TroopType from ITroop (works for both)
        if (troop is Troop unityTroop && unityTroop.TroopStats != null)
        {
            TroopType = unityTroop.TroopStats.TroopType;
            TeamIndex = unityTroop.TeamIndex;
        }
        else if (troop is Troop_Atoms atomsTroop)
        {
            TroopType = atomsTroop.Stats.TroopType;
            TeamIndex = atomsTroop.TeamIndex;
        }

        Position = troop.Transform.position;
        Rotation = troop.Transform.rotation;
        CurrentHealth = troop.CurrentHealth;
    }
}