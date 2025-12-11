using UnityEngine;

/// <summary>
/// Atoms melee troop - stops completely before attacking
/// </summary>
public class CloseTroop_Atoms : Troop_Atoms
{
    public override void Attack()
    {
        if (IsDead) return;
        if (Target == null || Target.IsDead) return;

        // Ensure completely stopped
        if (Agent != null && Agent.enabled && Agent.isOnNavMesh)
        {
            Agent.isStopped = true;
            Agent.velocity = Vector3.zero;
        }

        // Play animation and deal damage
        if (_animController != null)
        {
            // Animation controller handles melee attacks
            _animController.PlayAttackAnimation();
        }

        Target.TakeDamage(GetDamage());
    }
}