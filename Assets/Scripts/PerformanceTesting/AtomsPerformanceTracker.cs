using UnityEngine;
using Unity.Profiling;

/// <summary>
/// Lightweight wrapper to track Atoms-specific performance metrics
/// Integrated into Atoms Variable/Event operations
/// </summary>
public static class AtomsPerformanceTracker
{
    // Profiler markers for Unity Profiler integration
    private static readonly ProfilerMarker VariableReadMarker = new ProfilerMarker("Atoms.Variable.Read");
    private static readonly ProfilerMarker VariableWriteMarker = new ProfilerMarker("Atoms.Variable.Write");
    private static readonly ProfilerMarker EventRaiseMarker = new ProfilerMarker("Atoms.Event.Raise");
    private static readonly ProfilerMarker ListenerInvokeMarker = new ProfilerMarker("Atoms.Listener.Invoke");

    // Only track if profiler is running
    private static bool IsProfilingEnabled => PerformanceProfiler.Instance != null && PerformanceProfiler.Instance.enabled;

    /// <summary>
    /// Track a variable read operation
    /// Call this in your custom Atoms variable getters
    /// </summary>
    public static void TrackVariableRead()
    {
        if (!IsProfilingEnabled) return;

        VariableReadMarker.Begin();
        PerformanceProfiler.Instance.RecordAtomsVariableRead();
        VariableReadMarker.End();
    }

    /// <summary>
    /// Track a variable write operation
    /// Call this in your custom Atoms variable setters
    /// </summary>
    public static void TrackVariableWrite()
    {
        if (!IsProfilingEnabled) return;

        VariableWriteMarker.Begin();
        PerformanceProfiler.Instance.RecordAtomsVariableWrite();
        VariableWriteMarker.End();
    }

    /// <summary>
    /// Track an event dispatch
    /// Call this in your custom Atoms event Raise() methods
    /// </summary>
    public static void TrackEventDispatch()
    {
        if (!IsProfilingEnabled) return;

        EventRaiseMarker.Begin();
        PerformanceProfiler.Instance.RecordAtomsEventDispatch();
        EventRaiseMarker.End();
    }

    /// <summary>
    /// Track memory allocated by Atoms (e.g., listener registration)
    /// </summary>
    public static void TrackAllocation(long bytes)
    {
        if (!IsProfilingEnabled) return;

        PerformanceProfiler.Instance.RecordAtomsAllocation(bytes);
    }

    /// <summary>
    /// Begin tracking a listener invocation
    /// </summary>
    public static void BeginListenerInvoke()
    {
        if (!IsProfilingEnabled) return;
        ListenerInvokeMarker.Begin();
    }

    /// <summary>
    /// End tracking a listener invocation
    /// </summary>
    public static void EndListenerInvoke()
    {
        if (!IsProfilingEnabled) return;
        ListenerInvokeMarker.End();
    }
}