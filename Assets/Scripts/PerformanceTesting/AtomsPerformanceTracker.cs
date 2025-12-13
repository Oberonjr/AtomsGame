using UnityEngine;
using Unity.Profiling;
using System.Diagnostics;

/// <summary>
/// Enhanced performance tracker for Atoms-specific operations with high-precision timing
/// Integrated into Atoms Variable/Event operations
/// </summary>
public static class AtomsPerformanceTracker
{
    // Profiler markers for Unity Profiler integration
    private static readonly ProfilerMarker VariableReadMarker = new ProfilerMarker("Atoms.Variable.Read");
    private static readonly ProfilerMarker VariableWriteMarker = new ProfilerMarker("Atoms.Variable.Write");
    private static readonly ProfilerMarker EventRaiseMarker = new ProfilerMarker("Atoms.Event.Raise");
    private static readonly ProfilerMarker EventPropagationMarker = new ProfilerMarker("Atoms.Event.Propagation");
    private static readonly ProfilerMarker ListenerInvokeMarker = new ProfilerMarker("Atoms.Listener.Invoke");
    private static readonly ProfilerMarker InstancerCreateMarker = new ProfilerMarker("Atoms.Instancer.Create");
    private static readonly ProfilerMarker CollectionModifyMarker = new ProfilerMarker("Atoms.Collection.Modify");

    // High-precision stopwatch for microsecond timing
    private static Stopwatch _stopwatch = new Stopwatch();
    
    // Cascading write tracking
    private static int _currentWriteDepth = 0;
    private static int _maxWriteDepth = 0;

    // Only track if profiler is running
    private static bool IsProfilingEnabled => 
        PerformanceProfiler.Instance != null && 
        PerformanceProfiler.Instance.enabled;

    // ========== VARIABLE ACCESS TRACKING ==========

    /// <summary>
    /// Track a variable read operation (simple - no timing)
    /// Call this in Atoms variable getters for lightweight tracking
    /// </summary>
    public static void TrackVariableRead()
    {
        if (!IsProfilingEnabled) return;

        VariableReadMarker.Begin();
        PerformanceProfiler.Instance.RecordAtomsVariableRead();
        VariableReadMarker.End();
    }

    /// <summary>
    /// Track a variable read with high-precision timing
    /// Use this for detailed performance analysis
    /// </summary>
    public static void TrackVariableReadWithTiming(System.Action readAction)
    {
        if (!IsProfilingEnabled)
        {
            readAction?.Invoke();
            return;
        }

        VariableReadMarker.Begin();
        
        _stopwatch.Restart();
        readAction?.Invoke();
        _stopwatch.Stop();
        
        long microseconds = (_stopwatch.ElapsedTicks * 1000000) / Stopwatch.Frequency;
        PerformanceProfiler.Instance.RecordAtomsVariableReadWithTiming(microseconds);
        
        VariableReadMarker.End();
    }

    /// <summary>
    /// Track a variable write operation (simple - no timing)
    /// Call this in Atoms variable setters
    /// </summary>
    public static void TrackVariableWrite()
    {
        if (!IsProfilingEnabled) return;

        VariableWriteMarker.Begin();
        PerformanceProfiler.Instance.RecordAtomsVariableWrite();
        VariableWriteMarker.End();
    }

    /// <summary>
    /// Track a variable write with high-precision timing
    /// Automatically detects cascading writes
    /// </summary>
    public static void TrackVariableWriteWithTiming(System.Action writeAction)
    {
        if (!IsProfilingEnabled)
        {
            writeAction?.Invoke();
            return;
        }

        // Detect cascading writes
        _currentWriteDepth++;
        if (_currentWriteDepth > 1)
        {
            PerformanceProfiler.Instance.RecordCascadingWrite();
        }
        if (_currentWriteDepth > _maxWriteDepth)
        {
            _maxWriteDepth = _currentWriteDepth;
        }

        VariableWriteMarker.Begin();
        
        _stopwatch.Restart();
        writeAction?.Invoke();
        _stopwatch.Stop();
        
        long microseconds = (_stopwatch.ElapsedTicks * 1000000) / Stopwatch.Frequency;
        PerformanceProfiler.Instance.RecordAtomsVariableWriteWithTiming(microseconds);
        
        VariableWriteMarker.End();
        
        _currentWriteDepth--;
    }

    // ========== EVENT DISPATCH TRACKING ==========

    /// <summary>
    /// Track an event dispatch (simple - no timing)
    /// Call this in Atoms event Raise() methods
    /// </summary>
    public static void TrackEventDispatch()
    {
        if (!IsProfilingEnabled) return;

        EventRaiseMarker.Begin();
        PerformanceProfiler.Instance.RecordAtomsEventDispatch();
        EventRaiseMarker.End();
    }

    /// <summary>
    /// Track an event dispatch with high-precision timing
    /// Measures total propagation time including all listeners
    /// </summary>
    public static void TrackEventDispatchWithTiming(System.Action dispatchAction)
    {
        if (!IsProfilingEnabled)
        {
            dispatchAction?.Invoke();
            return;
        }

        EventRaiseMarker.Begin();
        EventPropagationMarker.Begin();
        
        _stopwatch.Restart();
        dispatchAction?.Invoke();
        _stopwatch.Stop();
        
        long microseconds = (_stopwatch.ElapsedTicks * 1000000) / Stopwatch.Frequency;
        PerformanceProfiler.Instance.RecordAtomsEventDispatchWithTiming(microseconds);
        
        EventPropagationMarker.End();
        EventRaiseMarker.End();
    }

    /// <summary>
    /// Track event propagation latency (time from cause to effect)
    /// Call at the start of a causal action, get token, then measure at effect
    /// </summary>
    public static EventLatencyToken StartEventLatencyTracking()
    {
        if (!IsProfilingEnabled)
        {
            return new EventLatencyToken { IsValid = false };
        }

        return new EventLatencyToken
        {
            IsValid = true,
            StartTime = Time.realtimeSinceStartup
        };
    }

    public static void EndEventLatencyTracking(EventLatencyToken token)
    {
        if (!IsProfilingEnabled || !token.IsValid) return;

        float latencyMs = (Time.realtimeSinceStartup - token.StartTime) * 1000f;
        PerformanceProfiler.Instance.RecordEventLatency(latencyMs);
    }

    public struct EventLatencyToken
    {
        public bool IsValid;
        public float StartTime;
    }

    // ========== LISTENER TRACKING ==========

    /// <summary>
    /// Begin tracking a listener invocation
    /// Call at the start of listener callback
    /// </summary>
    public static void BeginListenerInvoke()
    {
        if (!IsProfilingEnabled) return;
        
        ListenerInvokeMarker.Begin();
        PerformanceProfiler.Instance.RecordAtomsListenerInvocation();
    }

    /// <summary>
    /// End tracking a listener invocation
    /// Call at the end of listener callback
    /// </summary>
    public static void EndListenerInvoke()
    {
        if (!IsProfilingEnabled) return;
        ListenerInvokeMarker.End();
    }

    // ========== INSTANCER TRACKING ==========

    /// <summary>
    /// Track instancer creation with timing
    /// Call when creating a VariableInstancer
    /// </summary>
    public static void TrackInstancerCreation(System.Action createAction)
    {
        if (!IsProfilingEnabled)
        {
            createAction?.Invoke();
            return;
        }

        InstancerCreateMarker.Begin();
        
        _stopwatch.Restart();
        createAction?.Invoke();
        _stopwatch.Stop();
        
        float milliseconds = (_stopwatch.ElapsedTicks * 1000f) / Stopwatch.Frequency;
        PerformanceProfiler.Instance.RecordInstancerCreated(milliseconds);
        
        InstancerCreateMarker.End();
    }

    /// <summary>
    /// Track instancer destruction
    /// </summary>
    public static void TrackInstancerDestruction()
    {
        if (!IsProfilingEnabled) return;
        PerformanceProfiler.Instance.RecordInstancerDestroyed();
    }

    // ========== COLLECTION TRACKING ==========

    /// <summary>
    /// Track adding to an Atoms collection with timing
    /// </summary>
    public static void TrackCollectionAdd(System.Action addAction, int collectionSize)
    {
        if (!IsProfilingEnabled)
        {
            addAction?.Invoke();
            return;
        }

        CollectionModifyMarker.Begin();
        
        _stopwatch.Restart();
        addAction?.Invoke();
        _stopwatch.Stop();
        
        long microseconds = (_stopwatch.ElapsedTicks * 1000000) / Stopwatch.Frequency;
        PerformanceProfiler.Instance.RecordCollectionAdd(microseconds, collectionSize);
        
        CollectionModifyMarker.End();
    }

    /// <summary>
    /// Track removing from an Atoms collection with timing
    /// </summary>
    public static void TrackCollectionRemove(System.Action removeAction)
    {
        if (!IsProfilingEnabled)
        {
            removeAction?.Invoke();
            return;
        }

        CollectionModifyMarker.Begin();
        
        _stopwatch.Restart();
        removeAction?.Invoke();
        _stopwatch.Stop();
        
        long microseconds = (_stopwatch.ElapsedTicks * 1000000) / Stopwatch.Frequency;
        PerformanceProfiler.Instance.RecordCollectionRemove(microseconds);
        
        CollectionModifyMarker.End();
    }

    // ========== MEMORY TRACKING ==========

    /// <summary>
    /// Track memory allocation by Atoms infrastructure
    /// Call after allocating listeners, delegates, etc.
    /// </summary>
    public static void TrackAllocation(long bytes)
    {
        if (!IsProfilingEnabled) return;
        PerformanceProfiler.Instance.RecordAllocation(bytes);
    }

    // ========== UTILITY METHODS ==========

    /// <summary>
    /// Get the maximum cascading write depth detected
    /// Useful for understanding variable dependency chains
    /// </summary>
    public static int GetMaxCascadingDepth()
    {
        return _maxWriteDepth;
    }

    /// <summary>
    /// Reset cascading depth tracking
    /// Call between test runs
    /// </summary>
    public static void ResetCascadingDepth()
    {
        _currentWriteDepth = 0;
        _maxWriteDepth = 0;
    }

    // ========== CONVENIENCE WRAPPERS ==========

    /// <summary>
    /// Convenience: Track a read operation and return its value
    /// Usage: int value = AtomsPerformanceTracker.TrackRead(() => myVariable.Value);
    /// </summary>
    public static T TrackRead<T>(System.Func<T> readFunc)
    {
        if (!IsProfilingEnabled)
        {
            return readFunc();
        }

        VariableReadMarker.Begin();
        
        _stopwatch.Restart();
        T result = readFunc();
        _stopwatch.Stop();
        
        long microseconds = (_stopwatch.ElapsedTicks * 1000000) / Stopwatch.Frequency;
        PerformanceProfiler.Instance.RecordAtomsVariableReadWithTiming(microseconds);
        
        VariableReadMarker.End();
        
        return result;
    }

    /// <summary>
    /// Convenience: Track a write operation
    /// Usage: AtomsPerformanceTracker.TrackWrite(() => myVariable.Value = 10);
    /// </summary>
    public static void TrackWrite(System.Action writeAction)
    {
        TrackVariableWriteWithTiming(writeAction);
    }

    /// <summary>
    /// Convenience: Track an event raise
    /// Usage: AtomsPerformanceTracker.TrackEvent(() => myEvent.Raise());
    /// </summary>
    public static void TrackEvent(System.Action eventAction)
    {
        TrackEventDispatchWithTiming(eventAction);
    }
}       