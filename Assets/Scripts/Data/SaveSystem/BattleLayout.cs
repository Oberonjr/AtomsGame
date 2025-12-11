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
    public string TroopTypeName; // CHANGED: Store TroopType enum as string
    public Vector3 Position;
    public Quaternion Rotation;
    public int TeamIndex;
    public int CurrentHealth;
}