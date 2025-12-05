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

    [Header("Combat Timing")]
    [Tooltip("Time delay before the first attack after acquiring a new target (prevents instant attacks)")]
    public float InitialAttackDelay = 0.3f; // Default to 0.3 seconds
}
