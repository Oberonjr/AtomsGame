using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class TroopSnapshot
{
    public TroopStats Stats;
    public Vector3 Position;
    public Quaternion Rotation;
    public Guid TeamID;
    public int CurrentHealth;

    public TroopSnapshot(Troop troop)
    {
        Stats = troop.TroopStats;
        Position = troop.transform.position;
        Rotation = troop.transform.rotation;
        TeamID = troop.TeamID;
        CurrentHealth = troop.CurrentHealth;
    }
}