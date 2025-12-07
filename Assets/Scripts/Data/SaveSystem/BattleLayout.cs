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
    public string TroopStatsName;
    public Vector3 Position;
    public Quaternion Rotation;
    public int TeamIndex; // CHANGED: From TeamID string to int
    public int CurrentHealth;
}