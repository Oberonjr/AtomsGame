using Unity.Profiling;
using UnityEngine;

/// <summary>
/// Custom profiler markers for detailed Unity Profiler integration
/// </summary>
public static class ProfilingMarkers
{
    // Combat
    public static readonly ProfilerMarker AttackMarker = new ProfilerMarker("Combat.Attack");
    public static readonly ProfilerMarker TakeDamageMarker = new ProfilerMarker("Combat.TakeDamage");
    public static readonly ProfilerMarker FindTargetMarker = new ProfilerMarker("Combat.FindTarget");

    // AI
    public static readonly ProfilerMarker FSMUpdateMarker = new ProfilerMarker("AI.FSMUpdate");
    public static readonly ProfilerMarker PathfindingMarker = new ProfilerMarker("AI.Pathfinding");

    // Team Management
    public static readonly ProfilerMarker RegisterUnitMarker = new ProfilerMarker("Team.RegisterUnit");
    public static readonly ProfilerMarker UnitDiedMarker = new ProfilerMarker("Team.UnitDied");

    // Atoms Specific
    public static readonly ProfilerMarker AtomsVariableReadMarker = new ProfilerMarker("Atoms.VariableRead");
    public static readonly ProfilerMarker AtomsVariableWriteMarker = new ProfilerMarker("Atoms.VariableWrite");
    public static readonly ProfilerMarker AtomsEventRaiseMarker = new ProfilerMarker("Atoms.EventRaise");
}

// Usage example in Troop.cs:
/*
public virtual void Attack()
{
    using (ProfilingMarkers.AttackMarker.Auto())
    {
        // Attack logic
    }
}
*/