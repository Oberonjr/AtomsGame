using UnityEngine;

/// <summary>
/// Interface for all troop implementations (Unity and Atoms)
/// </summary>
public interface ITroop
{
    // Identity
    GameObject GameObject { get; }
    Transform Transform { get; }
    int TeamIndex { get; set; } // CHANGED: Use int instead of Guid

    // Stats access
    TroopStats TroopStats { get; } // Deprecated for Atoms, but kept for compatibility
    int GetMaxHealth();
    int GetDamage();
    float GetAttackRange();
    float GetAttackCooldown();
    float GetMoveSpeed();
    float GetInitialAttackDelay();

    // State
    int CurrentHealth { get; set; }
    bool IsDead { get; }
    ITroop Target { get; set; }

    // Behavior
    void Initialize();
    void UpdateTroop();
    void SetTarget(ITroop target);
    bool IsInRange(ITroop target);
    void Attack();
    void TakeDamage(int damage);
    void Die();
}