using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Unity.Profiling;

/// <summary>
/// Comprehensive performance profiler for comparing Unity vs Atoms implementations
/// Tracks separate phases: Prep, Simulation, Win
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
    [SerializeField] private bool _showOnScreenStats = true;
    [SerializeField] private KeyCode _toggleStatsKey = KeyCode.F1;

    private SimulationMode _currentMode;
    private GameState _currentPhase = GameState.Prep;
    
    // Phase-specific tracking
    private Dictionary<GameState, PhaseData> _phaseData = new Dictionary<GameState, PhaseData>();
    private PhaseData _currentPhaseData;
    
    private float _lastSampleTime;
    private bool _isProfiling;
    private System.Diagnostics.Stopwatch _sessionTimer;
    private System.Diagnostics.Stopwatch _phaseTimer;

    // Atoms-specific tracking
    private int _scriptableObjectsLoaded;
    private int _atomsEventDispatches;
    private int _atomsVariableReads;
    private int _atomsVariableWrites;
    private long _atomsAllocationBytes;

    // Unity Profiler Markers
    private static readonly ProfilerMarker _eventDispatchMarker = new ProfilerMarker("Atoms.EventDispatch");
    private static readonly ProfilerMarker _variableReadMarker = new ProfilerMarker("Atoms.VariableRead");
    private static readonly ProfilerMarker _variableWriteMarker = new ProfilerMarker("Atoms.VariableWrite");

    private bool _showStats = true;
    private string _sessionId;

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
        
        // Initialize phase data
        _phaseData[GameState.Prep] = new PhaseData(GameState.Prep);
        _phaseData[GameState.Simulate] = new PhaseData(GameState.Simulate);
        _phaseData[GameState.Win] = new PhaseData(GameState.Win);
    }

    void OnEnable()
    {
        // Subscribe to scene changes
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnloaded;
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
        
        // Start profiling when entering game scene
        if (scene.name == "GameScene" && SimulationConfig.Instance != null)
        {
            StartProfiling(SimulationConfig.Instance.Mode);
        }
    }

    private void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
    {
        Debug.Log($"[PerformanceProfiler] Scene unloaded: {scene.name}");
        
        // Stop profiling when leaving game scene
        if (scene.name == "GameScene" && _isProfiling)
        {
            StopProfiling();
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

        // Update current phase data
        if (_currentPhaseData != null)
        {
            _currentPhaseData.FrameCount++;
            float deltaTime = Time.unscaledDeltaTime;
            _currentPhaseData.DeltaTimeSum += deltaTime;

            if (deltaTime < _currentPhaseData.MinFrameTime) _currentPhaseData.MinFrameTime = deltaTime;
            if (deltaTime > _currentPhaseData.MaxFrameTime) _currentPhaseData.MaxFrameTime = deltaTime;

            // Update peak memory
            long currentMemory = System.GC.GetTotalMemory(false);
            if (currentMemory > _currentPhaseData.PeakMemory)
            {
                _currentPhaseData.PeakMemory = currentMemory;
            }

            // Sample at interval
            if (Time.unscaledTime - _lastSampleTime >= _sampleInterval)
            {
                RecordSample();
                _lastSampleTime = Time.unscaledTime;
            }
        }
    }

    public void StartProfiling(SimulationMode mode)
    {
        if (!_enableProfiling) return;
        if (_isProfiling)
        {
            Debug.LogWarning("[PerformanceProfiler] Already profiling, stopping previous session");
            StopProfiling();
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

        // Atoms-specific initialization
        _scriptableObjectsLoaded = 0;
        _atomsEventDispatches = 0;
        _atomsVariableReads = 0;
        _atomsVariableWrites = 0;
        _atomsAllocationBytes = 0;

        if (mode == SimulationMode.Atoms)
        {
            CountScriptableObjects();
        }

        _lastSampleTime = Time.unscaledTime;
        _sessionTimer.Restart();
        _phaseTimer.Restart();
        _isProfiling = true;

        Debug.Log($"[PerformanceProfiler] Started profiling {mode} mode - Session ID: {_sessionId}");
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

        // Stop timer for current phase
        if (_currentPhaseData != null)
        {
            _currentPhaseData.Duration = _phaseTimer.Elapsed.TotalSeconds;
        }

        // Switch to new phase
        _currentPhase = newPhase;
        _currentPhaseData = _phaseData[newPhase];
        _phaseTimer.Restart();
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
            GCCount2 = System.GC.CollectionCount(2) - _currentPhaseData.StartGC2
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
        _scriptableObjectsLoaded = Resources.FindObjectsOfTypeAll<ScriptableObject>().Length;
    }

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
    }

    public void RecordAtomsVariableRead()
    {
        _atomsVariableReads++;
    }

    public void RecordAtomsVariableWrite()
    {
        _atomsVariableWrites++;
    }

    public void RecordAtomsAllocation(long bytes)
    {
        _atomsAllocationBytes += bytes;
    }

    public void ExportData()
    {
        string directory = Path.Combine(Application.persistentDataPath, _exportPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string baseFilename = $"{_currentMode}_{_sessionId}";

        // Export phase-specific CSVs
        foreach (var kvp in _phaseData)
        {
            if (kvp.Value.Samples.Count > 0)
            {
                string csvPath = Path.Combine(directory, $"{baseFilename}_{kvp.Key}.csv");
                ExportPhaseCSV(csvPath, kvp.Value);
            }
        }

        // Export comprehensive summary
        string summaryPath = Path.Combine(directory, $"{baseFilename}_Summary.txt");
        ExportComprehensiveSummary(summaryPath);

        // Export machine-readable JSON for comparison tool
        string jsonPath = Path.Combine(directory, $"{baseFilename}_Data.json");
        ExportJSON(jsonPath);

        Debug.Log($"[PerformanceProfiler] Exported all data to: {directory}");
    }

    private void ExportPhaseCSV(string filepath, PhaseData phase)
    {
        StringBuilder csv = new StringBuilder();
        csv.AppendLine("Timestamp,FPS,FrameTimeMs,MemoryMB,ActiveUnits,GC0,GC1,GC2");

        foreach (var sample in phase.Samples)
        {
            csv.AppendLine($"{sample.Timestamp:F3},{sample.FPS:F2},{sample.FrameTime:F3}," +
                          $"{sample.MemoryUsageMB:F2},{sample.ActiveUnits}," +
                          $"{sample.GCCount0},{sample.GCCount1},{sample.GCCount2}");
        }

        File.WriteAllText(filepath, csv.ToString());
    }

    private void ExportComprehensiveSummary(string filepath)
    {
        StringBuilder summary = new StringBuilder();

        summary.AppendLine("╔════════════════════════════════════════════════════════════════╗");
        summary.AppendLine("║          COMPREHENSIVE PERFORMANCE SUMMARY                     ║");
        summary.AppendLine("╚════════════════════════════════════════════════════════════════╝");
        summary.AppendLine();
        summary.AppendLine($"Mode: {_currentMode}");
        summary.AppendLine($"Session ID: {_sessionId}");
        summary.AppendLine($"Date: {System.DateTime.Now}");
        summary.AppendLine($"Total Duration: {_sessionTimer.Elapsed.TotalSeconds:F2} seconds");
        summary.AppendLine();

        // Per-phase summaries
        foreach (var kvp in _phaseData)
        {
            GameState phase = kvp.Key;
            PhaseData data = kvp.Value;

            if (data.Samples.Count == 0) continue;

            summary.AppendLine($"┌─────────────────────────────────────────────────────────────┐");
            summary.AppendLine($"│ PHASE: {phase,-53} │");
            summary.AppendLine($"└─────────────────────────────────────────────────────────────┘");
            summary.AppendLine($"Duration: {data.Duration:F2}s");
            summary.AppendLine();

            // FPS Statistics
            var fpsSamples = data.Samples.Select(s => s.FPS).ToList();
            summary.AppendLine("─── FPS Statistics ───");
            summary.AppendLine($"  Average: {fpsSamples.Average():F2}");
            summary.AppendLine($"  Min: {fpsSamples.Min():F2}");
            summary.AppendLine($"  Max: {fpsSamples.Max():F2}");
            summary.AppendLine($"  Median: {GetMedian(fpsSamples):F2}");
            summary.AppendLine($"  1% Low: {GetPercentile(fpsSamples, 0.01f):F2}");
            summary.AppendLine($"  0.1% Low: {GetPercentile(fpsSamples, 0.001f):F2}");
            summary.AppendLine();

            // Frame Time Statistics
            var frameTimeSamples = data.Samples.Select(s => s.FrameTime).ToList();
            summary.AppendLine("─── Frame Time (ms) ───");
            summary.AppendLine($"  Average: {frameTimeSamples.Average():F3}");
            summary.AppendLine($"  Min: {frameTimeSamples.Min():F3}");
            summary.AppendLine($"  Max: {frameTimeSamples.Max():F3}");
            summary.AppendLine($"  95th Percentile: {GetPercentile(frameTimeSamples, 0.95f):F3}");
            summary.AppendLine($"  99th Percentile: {GetPercentile(frameTimeSamples, 0.99f):F3}");
            summary.AppendLine();

            // Memory Statistics
            var memorySamples = data.Samples.Select(s => s.MemoryUsageMB).ToList();
            summary.AppendLine("─── Memory (MB) ───");
            summary.AppendLine($"  Start: {data.StartMemory / (1024f * 1024f):F2}");
            summary.AppendLine($"  Peak: {data.PeakMemory / (1024f * 1024f):F2}");
            summary.AppendLine($"  Average: {memorySamples.Average():F2}");
            summary.AppendLine($"  Allocated: {(data.PeakMemory - data.StartMemory) / (1024f * 1024f):F2}");
            summary.AppendLine();

            // GC Statistics
            summary.AppendLine("─── Garbage Collection ───");
            summary.AppendLine($"  Gen 0: {data.Samples.LastOrDefault().GCCount0}");
            summary.AppendLine($"  Gen 1: {data.Samples.LastOrDefault().GCCount1}");
            summary.AppendLine($"  Gen 2: {data.Samples.LastOrDefault().GCCount2}");
            summary.AppendLine();

            // Combat Statistics (only for Simulate phase)
            if (phase == GameState.Simulate)
            {
                summary.AppendLine("─── Combat Statistics ───");
                summary.AppendLine($"  Units Spawned: {data.TotalUnitsSpawned}");
                summary.AppendLine($"  Units Died: {data.TotalUnitsDied}");
                summary.AppendLine($"  Total Attacks: {data.TotalAttacks}");
                summary.AppendLine($"  Total Damage: {data.TotalDamageDealt}");
                if (data.TotalAttacks > 0)
                {
                    summary.AppendLine($"  Avg Damage/Attack: {(float)data.TotalDamageDealt / data.TotalAttacks:F2}");
                }
                summary.AppendLine();
            }

            summary.AppendLine();
        }

        // Atoms-specific metrics
        if (_currentMode == SimulationMode.Atoms)
        {
            summary.AppendLine("┌─────────────────────────────────────────────────────────────┐");
            summary.AppendLine("│ ATOMS-SPECIFIC METRICS                                      │");
            summary.AppendLine("└─────────────────────────────────────────────────────────────┘");
            summary.AppendLine($"  ScriptableObjects Loaded: {_scriptableObjectsLoaded}");
            summary.AppendLine($"  Event Dispatches: {_atomsEventDispatches}");
            summary.AppendLine($"  Variable Reads: {_atomsVariableReads}");
            summary.AppendLine($"  Variable Writes: {_atomsVariableWrites}");
            summary.AppendLine($"  Atoms Allocations: {_atomsAllocationBytes / (1024f * 1024f):F2} MB");
            
            // Calculate overhead per operation
            PhaseData simPhase = _phaseData[GameState.Simulate];
            if (simPhase.Samples.Count > 0)
            {
                float avgFrameTime = simPhase.Samples.Select(s => s.FrameTime).Average();
                float eventOverhead = _atomsEventDispatches > 0 ? (avgFrameTime / _atomsEventDispatches) : 0f;
                summary.AppendLine($"  Avg Event Overhead: {eventOverhead:F6} ms/event");
            }
            summary.AppendLine();
        }

        File.WriteAllText(filepath, summary.ToString());
    }

    private void ExportJSON(string filepath)
    {
        var exportData = new SessionExportData
        {
            Mode = _currentMode.ToString(),
            SessionId = _sessionId,
            TotalDuration = _sessionTimer.Elapsed.TotalSeconds,
            Phases = new List<PhaseExportData>()
        };

        foreach (var kvp in _phaseData)
        {
            if (kvp.Value.Samples.Count == 0) continue;

            var phaseExport = new PhaseExportData
            {
                Phase = kvp.Key.ToString(),
                Duration = kvp.Value.Duration,
                AvgFPS = kvp.Value.Samples.Select(s => s.FPS).Average(),
                MinFPS = kvp.Value.Samples.Select(s => s.FPS).Min(),
                MaxFPS = kvp.Value.Samples.Select(s => s.FPS).Max(),
                AvgFrameTime = kvp.Value.Samples.Select(s => s.FrameTime).Average(),
                PeakMemoryMB = kvp.Value.PeakMemory / (1024f * 1024f),
                GC0 = kvp.Value.Samples.LastOrDefault().GCCount0,
                GC1 = kvp.Value.Samples.LastOrDefault().GCCount1,
                GC2 = kvp.Value.Samples.LastOrDefault().GCCount2,
                TotalAttacks = kvp.Value.TotalAttacks,
                TotalDamage = kvp.Value.TotalDamageDealt,
                UnitsSpawned = kvp.Value.TotalUnitsSpawned,
                UnitsDied = kvp.Value.TotalUnitsDied
            };

            exportData.Phases.Add(phaseExport);
        }

        // Atoms-specific data
        if (_currentMode == SimulationMode.Atoms)
        {
            exportData.AtomsMetrics = new AtomsMetricsData
            {
                ScriptableObjectsLoaded = _scriptableObjectsLoaded,
                EventDispatches = _atomsEventDispatches,
                VariableReads = _atomsVariableReads,
                VariableWrites = _atomsVariableWrites,
                AllocationBytes = _atomsAllocationBytes
            };
        }

        string json = JsonUtility.ToJson(exportData, true);
        File.WriteAllText(filepath, json);
    }

    private float GetMedian(List<float> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        int mid = sorted.Count / 2;
        return sorted.Count % 2 == 0 ? (sorted[mid - 1] + sorted[mid]) / 2f : sorted[mid];
    }

    private float GetPercentile(List<float> values, float percentile)
    {
        var sorted = values.OrderBy(v => v).ToList();
        int index = Mathf.Clamp((int)(sorted.Count * percentile), 0, sorted.Count - 1);
        return sorted[index];
    }

    void OnGUI()
    {
        if (!_showOnScreenStats || !_showStats || !_isProfiling || _currentPhaseData == null) return;

        int yOffset = 10;
        int lineHeight = 20;
        int xOffset = 10;
        int width = 400;

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.8f));

        GUI.Box(new Rect(5, 5, width, 280), "", boxStyle);

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 12;
        labelStyle.normal.textColor = Color.white;
        labelStyle.fontStyle = FontStyle.Bold;

        // Title
        GUI.Label(new Rect(xOffset, yOffset, width, lineHeight),
                  $"<b><size=14>Performance Stats - {_currentMode} - {_currentPhase}</size></b>", labelStyle);
        yOffset += lineHeight + 5;

        // Current FPS
        float currentFPS = 1f / Time.unscaledDeltaTime;
        Color fpsColor = currentFPS >= 60 ? Color.green : currentFPS >= 30 ? Color.yellow : Color.red;
        labelStyle.normal.textColor = fpsColor;
        GUI.Label(new Rect(xOffset, yOffset, width, lineHeight),
                  $"FPS: {currentFPS:F1}", labelStyle);
        yOffset += lineHeight;

        labelStyle.normal.textColor = Color.white;

        // Frame time
        GUI.Label(new Rect(xOffset, yOffset, width, lineHeight),
                  $"Frame Time: {Time.unscaledDeltaTime * 1000f:F2} ms", labelStyle);
        yOffset += lineHeight;

        // Memory
        float memoryMB = System.GC.GetTotalMemory(false) / (1024f * 1024f);
        GUI.Label(new Rect(xOffset, yOffset, width, lineHeight),
                  $"Memory: {memoryMB:F2} MB (Peak: {_currentPhaseData.PeakMemory / (1024f * 1024f):F2})", labelStyle);
        yOffset += lineHeight;

        // Active units
        int activeUnits = GetActiveUnitCount();
        GUI.Label(new Rect(xOffset, yOffset, width, lineHeight),
                  $"Active Units: {activeUnits}", labelStyle);
        yOffset += lineHeight;

        // Phase duration
        GUI.Label(new Rect(xOffset, yOffset, width, lineHeight),
                  $"Phase Time: {_phaseTimer.Elapsed.TotalSeconds:F1}s", labelStyle);
        yOffset += lineHeight;

        // Session duration
        GUI.Label(new Rect(xOffset, yOffset, width, lineHeight),
                  $"Session Time: {_sessionTimer.Elapsed.TotalSeconds:F1}s", labelStyle);
        yOffset += lineHeight;

        // Statistics (if samples available)
        if (_currentPhaseData.Samples.Count > 0)
        {
            var recentSamples = _currentPhaseData.Samples.TakeLast(100).ToList();
            float avgFPS = recentSamples.Select(s => s.FPS).Average();
            float minFPS = recentSamples.Select(s => s.FPS).Min();

            GUI.Label(new Rect(xOffset, yOffset, width, lineHeight),
                      $"Avg FPS (recent): {avgFPS:F1}", labelStyle);
            yOffset += lineHeight;

            GUI.Label(new Rect(xOffset, yOffset, width, lineHeight),
                      $"Min FPS (recent): {minFPS:F1}", labelStyle);
            yOffset += lineHeight;
        }

        // Combat stats
        if (_currentPhase == GameState.Simulate)
        {
            GUI.Label(new Rect(xOffset, yOffset, width, lineHeight),
                      $"Attacks: {_currentPhaseData.TotalAttacks}", labelStyle);
            yOffset += lineHeight;

            GUI.Label(new Rect(xOffset, yOffset, width, lineHeight),
                      $"Damage: {_currentPhaseData.TotalDamageDealt}", labelStyle);
            yOffset += lineHeight;
        }

        // Atoms metrics
        if (_currentMode == SimulationMode.Atoms)
        {
            GUI.Label(new Rect(xOffset, yOffset, width, lineHeight),
                      $"Atoms Events: {_atomsEventDispatches}", labelStyle);
            yOffset += lineHeight;
        }

        // Toggle hint
        labelStyle.fontSize = 10;
        GUI.Label(new Rect(xOffset, yOffset, width, lineHeight),
                  $"Press {_toggleStatsKey} to toggle", labelStyle);
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    // Data structures
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
        public string Phase;
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
    }

    [System.Serializable]
    private class AtomsMetricsData
    {
        public int ScriptableObjectsLoaded;
        public int EventDispatches;
        public int VariableReads;
        public int VariableWrites;
        public long AllocationBytes;
    }
}