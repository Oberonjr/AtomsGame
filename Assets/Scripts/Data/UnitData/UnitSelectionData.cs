using UnityEngine;

[CreateAssetMenu(fileName = "Unit Selection Data", menuName = "Unit Selection")]
public class UnitSelectionData : ScriptableObject
{
    [Header("Display")]
    public string DisplayName;
    public Sprite Icon;
    
    [Header("Unity Implementation")]
    public Troop TroopPrefab;
    
    [Header("Atoms Implementation")]    
    public Troop_Atoms TroopPrefab_Atoms;
}

