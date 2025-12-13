using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Unity.Profiling;
using System;
using System.Runtime.Serialization;

/// <summary>
/// Comprehensive performance profiler for comparing Unity vs Atoms implementations
/// Tracks separate phases: Prep, Simulation, Win with deep metrics
/// </summary>
public class PerformanceProfiler : MonoBehaviour
{
    private static PerformanceProfiler _instance;
    public static PerformanceProfiler Instance => _instance;

    [Header("Profiling Settings")]
    [SerializeField] private bool _enableProfiling = true;
    [SerializeField] private float _sampleInterval = 0.1f;
    [SerializeField] private bool _autoExportOnEnd = true;
    [SerializeField] private string _exportPath = "PerformanceData";

    [Header("Display")]
    [SerializeField] private KeyCode _toggleStatsKey = KeyCode.F1;

    [Header("Deep Profiling")]
    [SerializeField] private bool _trackWarmupPhase = true;

    private SimulationMode _currentMode;
    private GameState _currentPhase = GameState.Prep;
    
    // Phase-specific tracking
    private Dictionary<GameState, PhaseData> _phaseData = new Dictionary<GameState, PhaseData>();
    private PhaseData _currentPhaseData;
    
    private float _lastSampleTime;
    private bool _isProfiling;
    private System.Diagnostics.Stopwatch _sessionTimer;
    private System.Diagnostics.Stopwatch _phaseTimer;
    private System.Diagnostics.Stopwatch _warmupTimer;

    // ========== ATOMS-SPECIFIC TRACKING ==========
    
    // 1. Event System Metrics
    private int _atomsEventDispatches;
    private int _atomsEventListenerInvocations;
    private long _totalEventDispatchTime; // Microseconds
    private long _minEventDispatchTime = long.MaxValue;
    private long _maxEventDispatchTime = long.MinValue;
    private Dictionary<string, int> _eventFrequencyPerFrame = new Dictionary<string, int>();
    private int _eventsThisFrame;
    private List<float> _eventSpikeHistory = new List<float>();
    
    // 2. Variable Access Metrics
    private int _atomsVariableReads;
    private int _atomsVariableWrites;
    private long _totalVariableReadTime; // Microseconds
    private long _totalVariableWriteTime;
    private int _cascadingWrites; // Writes that trigger other writes
    private int _chainedDependencies; // Max depth of variable dependency
    
    // 3. ScriptableObject Metrics
    private int _scriptableObjectsLoaded;
    private float _scriptableObjectLoadTime;
    private long _scriptableObjectMemoryFootprint;
    
    // 4. Instancer Metrics
    private int _instancersCreated;
    private float _instancerCreationTime;
    private long _instancerMemoryPerUnit;
    private int _instancerDestroyCount;
    
    // 5. Collection Metrics (Atoms Lists/Dicts)
    private int _atomsCollectionAdds;
    private int _atomsCollectionRemoves;
    private long _totalCollectionAddTime;
    private long _totalCollectionRemoveTime;
    private Dictionary<int, float> _collectionSizePerformance = new Dictionary<int, float>();
    
    // 6. Memory Behavior
    private long _atomsAllocationBytes;
    private int _atomsGCSpikes; // Sharp GC increases
    private List<long> _memorySnapshots = new List<long>();
    private float _heapFragmentation; // Estimated
    
    // 7. Timing & Latency
    private List<float> _eventLatencies = new List<float>(); // Time from cause to effect
    private float _avgEventLatency;
    private float _maxEventLatency;
    
    // 8. Warm-up Metrics
    private bool _inWarmupPhase = true;
    private float _warmupDuration;
    private WarmupMetrics _warmupMetrics;
    
    // 9. Cache & Access Patterns
    private int _indirectionLookups;
    private long _totalIndirectionTime;

    // 10. Projectile Metrics (rockets, bullets, etc.)
    private int _projectilesSpawned;
    private int _projectileRetargets;
    private int _splashDamageHits; // Units hit by splash damage
    private int _totalSplashDamage;

    // 11. NavMesh Metrics
    private int _navMeshPathRecalculations;
    private int _navMeshAgentStucks;
    private float _totalNavMeshPathLength;

    // 12. FSM State Transition Metrics
    private Dictionary<string, int> _stateTransitions = new Dictionary<string, int>();
    private int _totalStateTransitions;

    // 13. Animation Metrics
    private int _animationTriggers;
    private Dictionary<string, int> _animationFrequency = new Dictionary<string, int>();

    // Unity Profiler Markers (Enhanced)
    private static readonly ProfilerMarker _eventDispatchMarker = new ProfilerMarker("Atoms.Event.Dispatch");
    private static readonly ProfilerMarker _eventPropagationMarker = new ProfilerMarker("Atoms.Event.Propagation");
    private static readonly ProfilerMarker _variableReadMarker = new ProfilerMarker("Atoms.Variable.Read");
    private static readonly ProfilerMarker _variableWriteMarker = new ProfilerMarker("Atoms.Variable.Write");
    private static readonly ProfilerMarker _instancerCreateMarker = new ProfilerMarker("Atoms.Instancer.Create");
    private static readonly ProfilerMarker _collectionModifyMarker = new ProfilerMarker("Atoms.Collection.Modify");
    private static readonly ProfilerMarker _listenerInvokeMarker = new ProfilerMarker("Atoms.Listener.Invoke");
    
    // Profiler Recorders (for Unity's internal metrics)
    private ProfilerRecorder _mainThreadTimeRecorder;
    private ProfilerRecorder _gcAllocRecorder;
    private ProfilerRecorder _drawCallsRecorder;
    private ProfilerRecorder _batchesRecorder;

    private bool _showStats = true;
    private string _sessionId;
    private string _profiledSceneName;

    // Add state entry counts
    private Dictionary<GameState, int> _stateEntryCounts = new Dictionary<GameState, int>();

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        _sessionTimer = new System.Diagnostics.Stopwatch();
        _phaseTimer = new System.Diagnostics.Stopwatch();
        _warmupTimer = new System.Diagnostics.Stopwatch();
        
        // Initialize phase data
        _phaseData[GameState.Prep] = new PhaseData(GameState.Prep);
        _phaseData[GameState.Simulate] = new PhaseData(GameState.Simulate);
        _phaseData[GameState.Win] = new PhaseData(GameState.Win);
        
        // Initialize state entry counts
        foreach (GameState gs in Enum.GetValues(typeof(GameState)))
            _stateEntryCounts[gs] =0;
        
        // Initialize profiler recorders
        _mainThreadTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
        _gcAllocRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc", 15);
        _drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 15);
        _batchesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count", 15);
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnloaded;
        
        // Dispose recorders
        _mainThreadTimeRecorder.Dispose();
        _gcAllocRecorder.Dispose();
        _drawCallsRecorder.Dispose();
        _batchesRecorder.Dispose();
    }

    void OnApplicationQuit()
    {
        if (_isProfiling)
        {
            StopProfiling();
        }
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        Debug.Log($"[PerformanceProfiler] Scene loaded: {scene.name}");
        
        // Start profiling when SimulationConfig is present in the loaded scene (assumes Sim runs in scenes with SimulationConfig)
        if (SimulationConfig.Instance != null)
        {
            StartProfiling(SimulationConfig.Instance.Mode);
        }
    }

    private void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
    {
        Debug.Log($"[PerformanceProfiler] Scene unloaded: {scene.name}");
        
        if (_isProfiling && !string.IsNullOrEmpty(_profiledSceneName) && scene.name == _profiledSceneName)
        {
            // Stop profiling and buffer export data
            StopProfiling();

            // Ensure buffered data is written now that profiled scene is unloading.
            try
            {
                SessionFileSaver.Instance.ForceSave();
                Debug.Log("[PerformanceProfiler] Forced SessionFileSaver to save buffered files on profiled scene unload.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PerformanceProfiler] Failed to force save session files: {e.Message}");
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(_toggleStatsKey))
        {
            _showStats = !_showStats;
        }

        if (!_enableProfiling || !_isProfiling) return;

        // Track phase changes
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != _currentPhase)
        {
            ChangePhase(GameStateManager.Instance.CurrentState);
        }

        // Check warm-up phase
        if (_inWarmupPhase && _warmupTimer.Elapsed.TotalSeconds >= 3.0)
        {
            EndWarmupPhase();
        }

        // Update current phase data
        if (_currentPhaseData != null)
        {
            _currentPhaseData.FrameCount++;
            float deltaTime = Time.unscaledDeltaTime;
            _currentPhaseData.DeltaTimeSum += deltaTime;

            if (deltaTime < _currentPhaseData.MinFrameTime) _currentPhaseData.MinFrameTime = deltaTime;
            if (deltaTime > _currentPhaseData.MaxFrameTime) _currentPhaseData.MaxFrameTime = deltaTime;

            // Track memory
            long currentMemory = System.GC.GetTotalMemory(false);
            if (currentMemory > _currentPhaseData.PeakMemory)
            {
                _currentPhaseData.PeakMemory = currentMemory;
            }
            
            // Track GC spikes
            int currentGC0 = System.GC.CollectionCount(0);
            if (currentGC0 > _currentPhaseData.LastGC0Count)
            {
                _atomsGCSpikes++;
                _currentPhaseData.LastGC0Count = currentGC0;
            }
            
            // Track memory snapshots for fragmentation estimation
            _memorySnapshots.Add(currentMemory);
            if (_memorySnapshots.Count > 100)
            {
                _memorySnapshots.RemoveAt(0);
            }

            // Sample at interval
            if (Time.unscaledTime - _lastSampleTime >= _sampleInterval)
            {
                RecordSample();
                _lastSampleTime = Time.unscaledTime;
            }
        }
        
        // Per-frame event tracking
        _eventsThisFrame = 0;
    }

    void LateUpdate()
    {
        // Record event spikes
        if (_eventsThisFrame > 0)
        {
            _eventSpikeHistory.Add(_eventsThisFrame);
            if (_eventSpikeHistory.Count > 1000)
            {
                _eventSpikeHistory.RemoveAt(0);
            }
        }
    }

    public void StartProfiling(SimulationMode mode)
    {
        if (!_enableProfiling) return;
        if (_isProfiling)
        {
            Debug.LogWarning("[PerformanceProfiler] Already profiling, stopping previous session");
            // CHANGED: Don't auto-export when starting a new session
            bool originalAutoExport = _autoExportOnEnd;
            _autoExportOnEnd = false;
            StopProfiling();
            _autoExportOnEnd = originalAutoExport;
        }

        _currentMode = mode;
        _sessionId = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        
        // Reset all phase data
        foreach (var phase in _phaseData.Values)
        {
            phase.Reset();
        }

        _currentPhase = GameState.Prep;
        _currentPhaseData = _phaseData[_currentPhase];

        // Reset all metrics
        ResetMetrics();

        // Mark first entry into starting phase
        if (!_stateEntryCounts.ContainsKey(_currentPhase))
            _stateEntryCounts[_currentPhase] = 0;
        _stateEntryCounts[_currentPhase]++;

        // Atoms-specific initialization
        if (mode == SimulationMode.Atoms)
        {
            CountScriptableObjects();
            StartWarmupTracking();
        }

        _lastSampleTime = Time.unscaledTime;
        _sessionTimer.Restart();
        _phaseTimer.Restart();
        _isProfiling = true;

        // Ensure SessionFileSaver exists early so it receives scene change events
        try
        {
            SessionFileSaver.Instance.BeginSession(_sessionId, mode);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PerformanceProfiler] Could not begin session in SessionFileSaver: {e.Message}");
        }

        // Remember which scene we're profiling so we can detect its unload
        _profiledSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        Debug.Log($"[PerformanceProfiler] Started profiling {mode} mode - Session ID: {_sessionId}");
    }

    private void ResetMetrics()
    {
        // Event metrics
        _atomsEventDispatches = 0;
        _atomsEventListenerInvocations = 0;
        _totalEventDispatchTime = 0;
        _minEventDispatchTime = long.MaxValue;
        _maxEventDispatchTime = long.MinValue;
        _eventFrequencyPerFrame.Clear();
        _eventsThisFrame = 0;
        _eventSpikeHistory.Clear();
        
        // Variable metrics
        _atomsVariableReads = 0;
        _atomsVariableWrites = 0;
        _totalVariableReadTime = 0;
        _totalVariableWriteTime = 0;
        _cascadingWrites = 0;
        _chainedDependencies = 0;
        
        // SO metrics
        _scriptableObjectsLoaded = 0;
        _scriptableObjectLoadTime = 0;
        _scriptableObjectMemoryFootprint = 0;
        
        // Instancer metrics
        _instancersCreated = 0;
        _instancerCreationTime = 0;
        _instancerMemoryPerUnit = 0;
        _instancerDestroyCount = 0;
        
        // Collection metrics
        _atomsCollectionAdds = 0;
        _atomsCollectionRemoves = 0;
        _totalCollectionAddTime = 0;
        _totalCollectionRemoveTime = 0;
        _collectionSizePerformance.Clear();
        
        // Memory metrics
        _atomsAllocationBytes = 0;
        _atomsGCSpikes = 0;
        _memorySnapshots.Clear();
        _heapFragmentation = 0;
        
        // Timing metrics
        _eventLatencies.Clear();
        _avgEventLatency = 0;
        _maxEventLatency = 0;
        
        // Cache metrics
        _indirectionLookups = 0;
        _totalIndirectionTime = 0;

        // Projectile metrics
        _projectilesSpawned = 0;
        _projectileRetargets = 0;
        _splashDamageHits = 0;
        _totalSplashDamage = 0;

        // NavMesh metrics
        _navMeshPathRecalculations = 0;
        _navMeshAgentStucks = 0;
        _totalNavMeshPathLength = 0;

        // FSM State Transition metrics
        _stateTransitions.Clear();
        _totalStateTransitions = 0;

        // Animation metrics
        _animationTriggers = 0;
        _animationFrequency.Clear();
    }

    private void StartWarmupTracking()
    {
        _inWarmupPhase = true;
        _warmupTimer.Restart();
        _warmupMetrics = new WarmupMetrics
        {
            StartTime = Time.realtimeSinceStartup,
            StartMemory = System.GC.GetTotalMemory(false),
            StartGC0 = System.GC.CollectionCount(0)
        };
    }

    private void EndWarmupPhase()
    {
        _inWarmupPhase = false;
        _warmupDuration = (float)_warmupTimer.Elapsed.TotalSeconds;
        
        _warmupMetrics.EndTime = Time.realtimeSinceStartup;
        _warmupMetrics.EndMemory = System.GC.GetTotalMemory(false);
        _warmupMetrics.EndGC0 = System.GC.CollectionCount(0);
        _warmupMetrics.Duration = _warmupDuration;
        
        Debug.Log($"[PerformanceProfiler] Warm-up phase ended after {_warmupDuration:F2}s");
    }

    public void StopProfiling()
    {
        if (!_isProfiling) return;

        _sessionTimer.Stop();
        _phaseTimer.Stop();
        _isProfiling = false;

        Debug.Log($"[PerformanceProfiler] Stopped profiling. Total Duration: {_sessionTimer.Elapsed.TotalSeconds:F2}s");

        if (_autoExportOnEnd)
        {
            ExportData();
        }
    }

    private void ChangePhase(GameState newPhase)
    {
        if (_currentPhase == newPhase) return;

        Debug.Log($"[PerformanceProfiler] Phase changed: {_currentPhase} -> {newPhase}");

        if (_currentPhaseData != null)
        {
            _currentPhaseData.Duration = _phaseTimer.Elapsed.TotalSeconds;
        }

        _currentPhase = newPhase;
        _currentPhaseData = _phaseData[newPhase];
        _phaseTimer.Restart();

        // Track how many times session entered each state
        if (!_stateEntryCounts.ContainsKey(newPhase))
            _stateEntryCounts[newPhase] =0;
        _stateEntryCounts[newPhase]++;
    }

    private void RecordSample()
    {
        if (_currentPhaseData == null) return;

        var sample = new PerformanceSample
        {
            Timestamp = Time.unscaledTime,
            FPS = 1f / Time.unscaledDeltaTime,
            FrameTime = Time.unscaledDeltaTime * 1000f,
            MemoryUsageMB = System.GC.GetTotalMemory(false) / (1024f * 1024f),
            ActiveUnits = GetActiveUnitCount(),
            GCCount0 = System.GC.CollectionCount(0) - _currentPhaseData.StartGC0,
            GCCount1 = System.GC.CollectionCount(1) - _currentPhaseData.StartGC1,
            GCCount2 = System.GC.CollectionCount(2) - _currentPhaseData.StartGC2,
            
            // Enhanced metrics
            EventsThisFrame = _eventsThisFrame,
            MainThreadTime = _mainThreadTimeRecorder.LastValue / 1000000f, // Convert to ms
            GCAllocThisFrame = _gcAllocRecorder.LastValue,
            DrawCalls = (int)_drawCallsRecorder.LastValue,
            Batches = (int)_batchesRecorder.LastValue
        };

        _currentPhaseData.Samples.Add(sample);
    }

    private int GetActiveUnitCount()
    {
        if (GameStateManager.Instance != null)
        {
            return GameStateManager.Instance.ActiveTroops.Count;
        }
        return 0;
    }

    private void CountScriptableObjects()
    {
        var allSOs = Resources.FindObjectsOfTypeAll<ScriptableObject>();
        _scriptableObjectsLoaded = allSOs.Length;
        
        // Estimate memory footprint
        _scriptableObjectMemoryFootprint = 0;
        foreach (var so in allSOs)
        {
            if (so != null)
            {
                // Rough estimate: 128 bytes per SO asset
                _scriptableObjectMemoryFootprint += 128;
            }
        }
    }

    // ========== PUBLIC TRACKING API ==========

    // Combat event tracking
    public void RecordDamage(int damage)
    {
        if (_currentPhaseData != null)
            _currentPhaseData.TotalDamageDealt += damage;
    }

    public void RecordUnitSpawned()
    {
        if (_currentPhaseData != null)
            _currentPhaseData.TotalUnitsSpawned++;
    }

    public void RecordUnitDeath()
    {
        if (_currentPhaseData != null)
            _currentPhaseData.TotalUnitsDied++;
    }

    public void RecordAttack()
    {
        if (_currentPhaseData != null)
            _currentPhaseData.TotalAttacks++;
    }

    // Atoms-specific tracking
    public void RecordAtomsEventDispatch()
    {
        _atomsEventDispatches++;
        _eventsThisFrame++;
    }

    public void RecordAtomsEventDispatchWithTiming(long microseconds)
    {
        _atomsEventDispatches++;
        _eventsThisFrame++;
        _totalEventDispatchTime += microseconds;
        
        if (microseconds < _minEventDispatchTime) _minEventDispatchTime = microseconds;
        if (microseconds > _maxEventDispatchTime) _maxEventDispatchTime = microseconds;
    }

    public void RecordAtomsListenerInvocation()
    {
        _atomsEventListenerInvocations++;
    }

    public void RecordAtomsVariableRead()
    {
        _atomsVariableReads++;
        _indirectionLookups++;
    }

    public void RecordAtomsVariableReadWithTiming(long microseconds)
    {
        _atomsVariableReads++;
        _indirectionLookups++;
        _totalVariableReadTime += microseconds;
        _totalIndirectionTime += microseconds;
    }

    public void RecordAtomsVariableWrite()
    {
        _atomsVariableWrites++;
    }

    public void RecordAtomsVariableWriteWithTiming(long microseconds)
    {
        _atomsVariableWrites++;
        _totalVariableWriteTime += microseconds;
    }

    public void RecordCascadingWrite()
    {
        _cascadingWrites++;
    }

    public void RecordAllocation(long bytes)
    {
        _atomsAllocationBytes += bytes;
    }

    public void RecordInstancerCreated(float creationTime)
    {
        _instancersCreated++;
        _instancerCreationTime += creationTime;
    }

    public void RecordInstancerDestroyed()
    {
        _instancerDestroyCount++;
    }

    public void RecordCollectionAdd(long microseconds, int collectionSize)
    {
        _atomsCollectionAdds++;
        _totalCollectionAddTime += microseconds;
        
        if (!_collectionSizePerformance.ContainsKey(collectionSize))
        {
            _collectionSizePerformance[collectionSize] = 0;
        }
        _collectionSizePerformance[collectionSize] += microseconds / 1000f; // Convert to ms
    }

    public void RecordCollectionRemove(long microseconds)
    {
        _atomsCollectionRemoves++;
        _totalCollectionRemoveTime += microseconds;
    }

    public void RecordEventLatency(float latencyMs)
    {
        _eventLatencies.Add(latencyMs);
        if (latencyMs > _maxEventLatency)
        {
            _maxEventLatency = latencyMs;
        }
    }

    public void RecordProjectileSpawned()
    {
        _projectilesSpawned++;
    }

    public void RecordProjectileRetarget()
    {
        _projectileRetargets++;
    }

    public void RecordSplashDamage(int unitsHit, int totalDamage)
    {
        _splashDamageHits += unitsHit;
        _totalSplashDamage += totalDamage;
    }

    public void RecordNavMeshRecalculation()
    {
        _navMeshPathRecalculations++;
    }

    public void RecordNavMeshStuck()
    {
        _navMeshAgentStucks++;
    }

    public void RecordNavMeshPathLength(float length)
    {
        _totalNavMeshPathLength += length;
    }

    public void RecordStateTransition(string fromState, string toState)
    {
        _totalStateTransitions++;
        string key = $"{fromState}→{toState}";
        
        if (!_stateTransitions.ContainsKey(key))
        {
            _stateTransitions[key] = 0;
        }
        _stateTransitions[key]++;
    }

    public void RecordAnimationTrigger(string animationName)
    {
        _animationTriggers++;
        
        if (!_animationFrequency.ContainsKey(animationName))
        {
            _animationFrequency[animationName] = 0;
        }
        _animationFrequency[animationName]++;
    }

    // Export methods will continue in next file...
    
    [System.Serializable]
    private class PhaseData
    {
        public GameState Phase;
        public List<PerformanceSample> Samples = new List<PerformanceSample>();
        public double Duration;
        
        public int FrameCount;
        public float DeltaTimeSum;
        public float MinFrameTime = float.MaxValue;
        public float MaxFrameTime = float.MinValue;
        
        public long StartMemory;
        public long PeakMemory;
        
        public int TotalDamageDealt;
        public int TotalUnitsSpawned;
        public int TotalUnitsDied;
        public int TotalAttacks;
        
        public int StartGC0;
        public int StartGC1;
        public int StartGC2;
        public int LastGC0Count; // For spike detection

        public PhaseData(GameState phase)
        {
            Phase = phase;
            Reset();
        }

        public void Reset()
        {
            Samples.Clear();
            Duration = 0;
            FrameCount = 0;
            DeltaTimeSum = 0;
            MinFrameTime = float.MaxValue;
            MaxFrameTime = float.MinValue;
            
            StartMemory = System.GC.GetTotalMemory(false);
            PeakMemory = StartMemory;
            
            TotalDamageDealt = 0;
            TotalUnitsSpawned = 0;
            TotalUnitsDied = 0;
            TotalAttacks = 0;
            
            StartGC0 = System.GC.CollectionCount(0);
            StartGC1 = System.GC.CollectionCount(1);
            StartGC2 = System.GC.CollectionCount(2);
            LastGC0Count = StartGC0;
        }
    }

    [System.Serializable]
    private struct PerformanceSample
    {
        public float Timestamp;
        public float FPS;
        public float FrameTime;
        public float MemoryUsageMB;
        public int ActiveUnits;
        public int GCCount0;
        public int GCCount1;
        public int GCCount2;
        
        // Enhanced metrics
        public int EventsThisFrame;
        public float MainThreadTime;
        public long GCAllocThisFrame;
        public int DrawCalls;
        public int Batches;
    }

    private struct WarmupMetrics
    {
        public float StartTime;
        public float EndTime;
        public float Duration;
        public long StartMemory;
        public long EndMemory;
        public int StartGC0;
        public int EndGC0;
    }

    public void ExportData()
    {
        // Build comprehensive summary string and JSON payload, then buffer via SessionFileSaver
        try
        {
            // Inline summary builder (kept concise here; detailed fields are in JSON)
            var sb = new StringBuilder();
            sb.AppendLine("╔════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║ COMPREHENSIVE PERFORMANCE SUMMARY ║");
            sb.AppendLine("╚════════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine($"Mode: {_currentMode}");
            sb.AppendLine($"Session ID: {_sessionId}");
            sb.AppendLine($"Date: {System.DateTime.Now}");
            sb.AppendLine($"Total Duration (session): {_sessionTimer.Elapsed.TotalSeconds:F2} seconds");
            sb.AppendLine();
            sb.AppendLine("--- State entry counts ---");
            sb.AppendLine($" Prep entered: {_stateEntryCounts[GameState.Prep]}");
            sb.AppendLine($" Simulate entered: {_stateEntryCounts[GameState.Simulate]}");
            sb.AppendLine($" Win entered: {_stateEntryCounts[GameState.Win]}");
            sb.AppendLine();
            
            string summary = sb.ToString();
            string json = BuildExportJson(summary);
            
            // Ensure SessionFileSaver session started
            try
            {
                SessionFileSaver.Instance.BeginSession(_sessionId, _currentMode);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PerformanceProfiler] Could not begin SessionFileSaver session: {e.Message}");
            }
            
            // Buffer JSON
            SessionFileSaver.Instance.SetPendingSessionJson(json);
            
            // Buffer summary text
            SessionFileSaver.Instance.BufferFile("Summary.txt", summary);
            
            // Buffer CSVs for each phase
            foreach (var kvp in _phaseData)
            {
                if (kvp.Value.Samples == null || kvp.Value.Samples.Count ==0) continue;
                string csv = BuildPhaseCSV(kvp.Key.ToString(), kvp.Value);
                SessionFileSaver.Instance.BufferFile($"{kvp.Key}.csv", csv);
            }
            
            // Buffering complete. Notify SessionFileSaver that buffered files are ready to be written
            // when the profiled scene unloads.
            try
            {
                SessionFileSaver.Instance.MarkReadyToSave();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PerformanceProfiler] Could not mark SessionFileSaver ready: {e.Message}");
            }
            
            Debug.Log("[PerformanceProfiler] Session JSON, summary and CSVs buffered for write on GameScene unload.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PerformanceProfiler] Failed to prepare session export: {e.Message}");
        }
    }

    // Build the single JSON payload (includes structured data + comprehensive summary string)
    private string BuildExportJson(string comprehensiveSummary)
    {
        var exportData = new SessionExportData
        {
            Mode = _currentMode.ToString(),
            SessionId = _sessionId,
            TotalDuration = _sessionTimer.Elapsed.TotalSeconds,
            Phases = new List<PhaseExportData>(),
            AtomsMetrics = null
        };

        foreach (var kvp in _phaseData)
        {
            if (kvp.Value.Samples.Count ==0) continue;

            var phaseExport = new PhaseExportData
            {
                Phase = kvp.Key.ToString(),
                Duration = kvp.Value.Duration,
                AvgFPS = kvp.Value.Samples.Select(s => s.FPS).Average(),
                MinFPS = kvp.Value.Samples.Select(s => s.FPS).Min(),
                MaxFPS = kvp.Value.Samples.Select(s => s.FPS).Max(),
                AvgFrameTime = kvp.Value.Samples.Select(s => s.FrameTime).Average(),
                PeakMemoryMB = kvp.Value.PeakMemory / (1024f *1024f),
                GC0 = kvp.Value.Samples.LastOrDefault().GCCount0,
                GC1 = kvp.Value.Samples.LastOrDefault().GCCount1,
                GC2 = kvp.Value.Samples.LastOrDefault().GCCount2,
                TotalAttacks = kvp.Value.TotalAttacks,
                TotalDamage = kvp.Value.TotalDamageDealt,
                UnitsSpawned = kvp.Value.TotalUnitsSpawned,
                UnitsDied = kvp.Value.TotalUnitsDied,

                AvgMainThreadTime = kvp.Value.Samples.Select(s => s.MainThreadTime).Average(),
                MaxMainThreadTime = kvp.Value.Samples.Select(s => s.MainThreadTime).Max(),
                TotalGCAlloc = kvp.Value.Samples.Sum(s => s.GCAllocThisFrame),
                AvgDrawCalls = (float)kvp.Value.Samples.Select(s => s.DrawCalls).Average(),
                AvgBatches = (float)kvp.Value.Samples.Select(s => s.Batches).Average(),

                ProjectilesSpawned = _projectilesSpawned,
                ProjectileRetargets = _projectileRetargets,
                SplashDamageHits = _splashDamageHits,
                TotalSplashDamage = _totalSplashDamage,
                NavMeshRecalculations = _navMeshPathRecalculations,
                NavMeshStucks = _navMeshAgentStucks,
                TotalPathLength = _totalNavMeshPathLength,
                StateTransitions = _totalStateTransitions,
                AnimationTriggers = _animationTriggers
            };

            exportData.Phases.Add(phaseExport);
        }

        if (_currentMode == SimulationMode.Atoms)
        {
            exportData.AtomsMetrics = new AtomsMetricsData
            {
                ScriptableObjectsLoaded = _scriptableObjectsLoaded,
                EventDispatches = _atomsEventDispatches,
                EventListenerInvocations = _atomsEventListenerInvocations,
                VariableReads = _atomsVariableReads,
                VariableWrites = _atomsVariableWrites,
                AllocationBytes = _atomsAllocationBytes,
                CascadingWrites = _cascadingWrites,
                GCSpikes = _atomsGCSpikes,
                AvgEventDispatchTime = _atomsEventDispatches >0 ? (_totalEventDispatchTime / (float)_atomsEventDispatches) :0,
                AvgVariableReadTime = _atomsVariableReads >0 ? (_totalVariableReadTime / (float)_atomsVariableReads) :0,
                AvgVariableWriteTime = _atomsVariableWrites >0 ? (_totalVariableWriteTime / (float)_atomsVariableWrites) :0,
                InstancersCreated = _instancersCreated,
                InstancerCreationTime = _instancerCreationTime,
                CollectionAdds = _atomsCollectionAdds,
                CollectionRemoves = _atomsCollectionRemoves,
                AvgEventLatency = _avgEventLatency,
                MaxEventLatency = _maxEventLatency,
                IndirectionLookups = _indirectionLookups,
                HeapFragmentation = _heapFragmentation,
                WarmupDuration = _warmupMetrics.Duration,
                WarmupMemoryAlloc = (_warmupMetrics.EndMemory - _warmupMetrics.StartMemory) / (1024f *1024f)
            };
        }

        // Build combined export object compatible with PerformanceComparison.SessionData but with extras
        var combined = new CombinedExport
        {
            Mode = exportData.Mode,
            SessionId = exportData.SessionId,
            TotalDuration = exportData.TotalDuration,
            Phases = exportData.Phases,
            AtomsMetrics = exportData.AtomsMetrics,
            ComprehensiveSummary = comprehensiveSummary,
            StateEntryCounts = new StateEntryCountsData
            {
                Prep = _stateEntryCounts.ContainsKey(GameState.Prep) ? _stateEntryCounts[GameState.Prep] :0,
                Simulate = _stateEntryCounts.ContainsKey(GameState.Simulate) ? _stateEntryCounts[GameState.Simulate] :0,
                Win = _stateEntryCounts.ContainsKey(GameState.Win) ? _stateEntryCounts[GameState.Win] :0
            }
        };

        string json = JsonUtility.ToJson(combined, true);
        return json;
    }

    private string BuildPhaseCSV(string phaseName, PhaseData phase)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Timestamp,FPS,FrameTimeMs,MemoryMB,ActiveUnits,GC0,GC1,GC2,EventsThisFrame,MainThreadTime,GCAllocThisFrame,DrawCalls,Batches");

        foreach (var sample in phase.Samples)
        {
            csv.AppendLine($"{sample.Timestamp:F3},{sample.FPS:F2},{sample.FrameTime:F3},{sample.MemoryUsageMB:F2},{sample.ActiveUnits},{sample.GCCount0},{sample.GCCount1},{sample.GCCount2},{sample.EventsThisFrame},{sample.MainThreadTime:F3},{sample.GCAllocThisFrame},{sample.DrawCalls},{sample.Batches}");
        }

        return csv.ToString();
    }

    [System.Serializable]
    private class CombinedExport
    {
        public string Mode;
        public string SessionId;
        public double TotalDuration;
        public List<PhaseExportData> Phases;
        public AtomsMetricsData AtomsMetrics;

        // Extras
        public string ComprehensiveSummary;
        public StateEntryCountsData StateEntryCounts;
    }

    [System.Serializable]
    private class StateEntryCountsData
    {
        public int Prep;
        public int Simulate;
        public int Win;
    }

    [System.Serializable]
    private class SessionExportData
    {
        public string Mode;
        public string SessionId;
        public double TotalDuration;
        public List<PhaseExportData> Phases;
        public AtomsMetricsData AtomsMetrics;
    }

    [System.Serializable]
    private class PhaseExportData
    {
        public string Phase;  // CHANGED: Was GameState, now string
        public double Duration;
        public float AvgFPS;
        public float MinFPS;
        public float MaxFPS;
        public float AvgFrameTime;
        public float PeakMemoryMB;
        public int GC0;
        public int GC1;
        public int GC2;
        public int TotalAttacks;
        public int TotalDamage;
        public int UnitsSpawned;
        public int UnitsDied;

        // Enhanced
        public float AvgMainThreadTime;
        public float MaxMainThreadTime;
        public long TotalGCAlloc;
        public float AvgDrawCalls;
        public float AvgBatches;
        
        // ADDED: Shared metrics (11-14)
        public int ProjectilesSpawned;
        public int ProjectileRetargets;
        public int SplashDamageHits;
        public int TotalSplashDamage;
        public int NavMeshRecalculations;
        public int NavMeshStucks;
        public float TotalPathLength;
        public int StateTransitions;
        public int AnimationTriggers;
    }

    [System.Serializable]
    private class AtomsMetricsData
    {
        public int ScriptableObjectsLoaded;
        public int EventDispatches;
        public int EventListenerInvocations;
        public int VariableReads;
        public int VariableWrites;
        public long AllocationBytes;
        public int CascadingWrites;
        public int GCSpikes;
        public float AvgEventDispatchTime;
        public float AvgVariableReadTime;
        public float AvgVariableWriteTime;
        public int InstancersCreated;
        public float InstancerCreationTime;
        public int CollectionAdds;
        public int CollectionRemoves;
        public float AvgEventLatency;
        public float MaxEventLatency;
        public int IndirectionLookups;
        public float HeapFragmentation;
        public float WarmupDuration;
        public float WarmupMemoryAlloc;
    }
}