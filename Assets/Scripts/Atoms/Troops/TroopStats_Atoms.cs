using System;
using UnityAtoms.BaseAtoms;

[Serializable]
public struct TroopStats_Atoms : IEquatable<TroopStats_Atoms>
{
    public TroopType TroopType;
    
    // These are REFERENCES to Atoms Variables, not the variables themselves
    public IntVariable MaxHealth;
    public IntVariable Damage;
    public FloatVariable AttackRange;
    public FloatVariable AttackCooldown;
    public FloatVariable MoveSpeed;
    public FloatVariable InitialAttackDelay;

    public bool Equals(TroopStats_Atoms other)
    {
        // Compare by reference equality (same variable assets)
        return TroopType == other.TroopType &&
               MaxHealth == other.MaxHealth &&
               Damage == other.Damage &&
               AttackRange == other.AttackRange &&
               AttackCooldown == other.AttackCooldown &&
               MoveSpeed == other.MoveSpeed &&
               InitialAttackDelay == other.InitialAttackDelay;
    }

    public override bool Equals(object obj)
    {
        return obj is TroopStats_Atoms other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TroopType, MaxHealth, Damage, AttackRange, AttackCooldown, MoveSpeed, InitialAttackDelay);
    }
}
