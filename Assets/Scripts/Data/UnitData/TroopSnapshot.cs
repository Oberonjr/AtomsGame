using System;
using UnityEngine;

[System.Serializable]
public class TroopSnapshot
{
    public TroopStats Stats;
    public Vector3 Position;
    public Quaternion Rotation;
    public int TeamIndex; // CHANGED: From Guid to int
    public int CurrentHealth;

    public TroopSnapshot(Troop troop)
    {
        Stats = troop.TroopStats;
        Position = troop.transform.position;
        Rotation = troop.transform.rotation;
        TeamIndex = troop.TeamIndex; // CHANGED: Use TeamIndex
        CurrentHealth = troop.CurrentHealth;
    }
}