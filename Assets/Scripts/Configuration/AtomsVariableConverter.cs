using UnityAtoms.BaseAtoms;
using UnityEngine;

/// <summary>
/// Static helper class for converting between Unity primitives and Atoms Variables
/// </summary>
public static class AtomsVariableConverter
{
    // ========== INT CONVERSIONS ==========

    /// <summary>
    /// Get int value from Atoms IntVariable with fallback
    /// </summary>
    public static int ToInt(IntVariable variable, int fallback = 0)
    {
        return variable?.Value ?? fallback;
    }

    /// <summary>
    /// Create or update IntVariable with given value
    /// </summary>
    public static IntVariable FromInt(int value, IntVariable existingVariable = null)
    {
        if (existingVariable != null)
        {
            existingVariable.SetValue(value);
            return existingVariable;
        }

        var newVariable = ScriptableObject.CreateInstance<IntVariable>();
        newVariable.SetValue(value);
        return newVariable;
    }

    // ========== FLOAT CONVERSIONS ==========

    /// <summary>
    /// Get float value from Atoms FloatVariable with fallback
    /// </summary>
    public static float ToFloat(FloatVariable variable, float fallback = 0f)
    {
        return variable?.Value ?? fallback;
    }

    /// <summary>
    /// Create or update FloatVariable with given value
    /// </summary>
    public static FloatVariable FromFloat(float value, FloatVariable existingVariable = null)
    {
        if (existingVariable != null)
        {
            existingVariable.SetValue(value);
            return existingVariable;
        }

        var newVariable = ScriptableObject.CreateInstance<FloatVariable>();
        newVariable.SetValue(value);
        return newVariable;
    }

    // ========== BOOL CONVERSIONS ==========

    /// <summary>
    /// Get bool value from Atoms BoolVariable with fallback
    /// </summary>
    public static bool ToBool(BoolVariable variable, bool fallback = false)
    {
        return variable?.Value ?? fallback;
    }

    /// <summary>
    /// Create or update BoolVariable with given value
    /// </summary>
    public static BoolVariable FromBool(bool value, BoolVariable existingVariable = null)
    {
        if (existingVariable != null)
        {
            existingVariable.SetValue(value);
            return existingVariable;
        }

        var newVariable = ScriptableObject.CreateInstance<BoolVariable>();
        newVariable.SetValue(value);
        return newVariable;
    }

    // ========== TROOPSTATS CONVERSIONS ==========

    /// <summary>
    /// Convert Unity TroopStats to Atoms TroopStats_Atoms struct
    /// Creates new Atoms Variables for each stat
    /// </summary>
    public static TroopStats_Atoms ToAtomsStats(TroopStats unityStats)
    {
        if (unityStats == null)
        {
            Debug.LogWarning("[AtomsVariableConverter] Attempted to convert null TroopStats");
            return new TroopStats_Atoms
            {
                TroopType = TroopType.MELEE,
                MaxHealth = FromInt(100),
                Damage = FromInt(10),
                AttackRange = FromFloat(2f),
                AttackCooldown = FromFloat(1f),
                MoveSpeed = FromFloat(3.5f),
                InitialAttackDelay = FromFloat(0.3f)
            };
        }

        return new TroopStats_Atoms
        {
            TroopType = unityStats.TroopType,
            MaxHealth = FromInt(unityStats.MaxHealth),
            Damage = FromInt(unityStats.Damage),
            AttackRange = FromFloat(unityStats.AttackRange),
            AttackCooldown = FromFloat(unityStats.AttackCooldown),
            MoveSpeed = FromFloat(unityStats.MoveSpeed),
            InitialAttackDelay = FromFloat(unityStats.InitialAttackDelay)
        };
    }

    /// <summary>
    /// Convert Atoms TroopStats_Atoms to Unity TroopStats
    /// Creates a runtime ScriptableObject (not saved to disk)
    /// </summary>
    public static TroopStats ToUnityStats(TroopStats_Atoms atomsStats)
    {
        var unityStats = ScriptableObject.CreateInstance<TroopStats>();
        unityStats.TroopType = atomsStats.TroopType;
        unityStats.MaxHealth = ToInt(atomsStats.MaxHealth, 100);
        unityStats.Damage = ToInt(atomsStats.Damage, 10);
        unityStats.AttackRange = ToFloat(atomsStats.AttackRange, 2f);
        unityStats.AttackCooldown = ToFloat(atomsStats.AttackCooldown, 1f);
        unityStats.MoveSpeed = ToFloat(atomsStats.MoveSpeed, 3.5f);
        unityStats.InitialAttackDelay = ToFloat(atomsStats.InitialAttackDelay, 0.3f);
        return unityStats;
    }

    /// <summary>
    /// Extract primitive values from TroopStats_Atoms as a tuple
    /// Useful for passing values without Atoms dependency
    /// </summary>
    public static (int maxHealth, int damage, float attackRange, float attackCooldown, float moveSpeed, float initialAttackDelay)
        ExtractPrimitives(TroopStats_Atoms atomsStats)
    {
        return (
            ToInt(atomsStats.MaxHealth, 100),
            ToInt(atomsStats.Damage, 10),
            ToFloat(atomsStats.AttackRange, 2f),
            ToFloat(atomsStats.AttackCooldown, 1f),
            ToFloat(atomsStats.MoveSpeed, 3.5f),
            ToFloat(atomsStats.InitialAttackDelay, 0.3f)
        );
    }

    /// <summary>
    /// Create TroopStats_Atoms from primitive values
    /// </summary>
    public static TroopStats_Atoms FromPrimitives(
        TroopType type,
        int maxHealth,
        int damage,
        float attackRange,
        float attackCooldown,
        float moveSpeed,
        float initialAttackDelay)
    {
        return new TroopStats_Atoms
        {
            TroopType = type,
            MaxHealth = FromInt(maxHealth),
            Damage = FromInt(damage),
            AttackRange = FromFloat(attackRange),
            AttackCooldown = FromFloat(attackCooldown),
            MoveSpeed = FromFloat(moveSpeed),
            InitialAttackDelay = FromFloat(initialAttackDelay)
        };
    }

    // ========== UTILITY METHODS ==========

    /// <summary>
    /// Check if Atoms stats struct has all variables initialized
    /// </summary>
    public static bool IsValidAtomsStats(TroopStats_Atoms atomsStats)
    {
        return atomsStats.MaxHealth != null &&
               atomsStats.Damage != null &&
               atomsStats.AttackRange != null &&
               atomsStats.AttackCooldown != null &&
               atomsStats.MoveSpeed != null &&
               atomsStats.InitialAttackDelay != null;
    }

    /// <summary>
    /// Log all values in Atoms stats for debugging
    /// </summary>
    public static void DebugLogAtomsStats(TroopStats_Atoms atomsStats, string prefix = "")
    {
        Debug.Log($"{prefix}TroopStats_Atoms:" +
                  $"\n  Type: {atomsStats.TroopType}" +
                  $"\n  MaxHealth: {ToInt(atomsStats.MaxHealth)}" +
                  $"\n  Damage: {ToInt(atomsStats.Damage)}" +
                  $"\n  AttackRange: {ToFloat(atomsStats.AttackRange)}" +
                  $"\n  AttackCooldown: {ToFloat(atomsStats.AttackCooldown)}" +
                  $"\n  MoveSpeed: {ToFloat(atomsStats.MoveSpeed)}" +
                  $"\n  InitialAttackDelay: {ToFloat(atomsStats.InitialAttackDelay)}");
    }
}