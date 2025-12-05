using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BattleLayout
{
    public string LayoutName;
    public string DateCreated;
    public List<SavedTroop> SavedTroops = new List<SavedTroop>();
}

[Serializable]
public class SavedTroop
{
    public string TroopStatsName; // Reference to TroopStats asset name
    public Vector3 Position;
    public Quaternion Rotation;
    public string TeamID; // Team GUID as string
    public int CurrentHealth;
}