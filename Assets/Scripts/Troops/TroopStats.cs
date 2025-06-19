using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TroopType
{
    MELEE,
    RANGED,
    ARTILLERY
}

[CreateAssetMenu(fileName = "Troop Stats", menuName = "Troop Stats")]
public class TroopStats : ScriptableObject
{
    public TroopType TroopType;
    public int MaxHealth;
    public int Damage;
    public float AttackRange;
    public float AttackCooldown;
    public float MoveSpeed;
}
