using UnityEngine;
using System;

/// <summary>
/// Interface for all troop implementations (Unity and Atoms)
/// </summary>
public interface ITroop
{
    // Identity
    GameObject GameObject { get; }
    Transform Transform { get; }
    Guid TeamID { get; set; }
    TroopStats TroopStats { get; }

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