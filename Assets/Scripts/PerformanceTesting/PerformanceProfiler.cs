using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

/// <summary>
/// Comprehensive performance profiler for comparing Unity vs Atoms implementations
/// </summary>
public class PerformanceProfiler : MonoBehaviour
{
    private static PerformanceProfiler _instance;
    public static PerformanceProfiler Instance => _instance;

    [Header("Profiling Settings")]
    [SerializeField] private bool _enableProfiling = true;
    [SerializeField] private float _sampleInterval = 0.1f; // Sample every 100ms
    [SerializeField] private bool _autoExportOnEnd = true;
    [SerializeField] private string _exportPath = "PerformanceData";

    [Header("Display")]
    [SerializeField] private bool _showOnScreenStats = true;
    [SerializeField] private KeyCode _toggleStatsKey = KeyCode.F1;

    private SimulationMode _currentMode;
    private List<PerformanceSample> _samples = new List<PerformanceSample>();
    private float _lastSampleTime;
    private bool _isProfiling;
    private System.Diagnostics.Stopwatch _simulationTimer;

    // Frame tracking
    private int _frameCount;
    private float _deltaTimeSum;
    private float _minFrameTime = float.MaxValue;
    private float _maxFrameTime = float.MinValue;

    // Memory tracking
    private long _startMemory;
    private long _peakMemory;

    // Combat tracking
    private int _totalDamageDealt;
    private int _totalUnitsSpawned;
    private int _totalUnitsDied;
    private int _totalAttacks;

    // GC tracking
    private int _gcCollectionCount0;
    private int _gcCollectionCount1;
    private int _gcCollectionCount2;

    private bool _showStats = true;

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

        _simulationTimer = new System.Diagnostics.Stopwatch();
    }

    void Update()
    {
        if (Input.GetKeyDown(_toggleStatsKey))
        {
            _showStats = !_showStats;
        }

        if (!_enableProfiling || !_isProfiling) return;

        _frameCount++;
        float deltaTime = Time.unscaledDeltaTime;
        _deltaTimeSum += deltaTime;

        if (deltaTime < _minFrameTime) _minFrameTime = deltaTime;
        if (deltaTime > _maxFrameTime) _maxFrameTime = deltaTime;

        // Update peak memory
        long currentMemory = System.GC.GetTotalMemory(false);
        if (currentMemory > _peakMemory)
        {
            _peakMemory = currentMemory;
        }

        // Sample at interval
        if (Time.unscaledTime - _lastSampleTime >= _sampleInterval)
        {
            RecordSample();
            _lastSampleTime = Time.unscaledTime;
        }
    }

    public void StartProfiling(SimulationMode mode)
    {
        if (!_enableProfiling) return;

        _currentMode = mode;
        _samples.Clear();
        _frameCount = 0;
        _deltaTimeSum = 0f;
        _minFrameTime = float.MaxValue;
        _maxFrameTime = float.MinValue;
        _totalDamageDealt = 0;
        _totalUnitsSpawned = 0;
        _totalUnitsDied = 0;
        _totalAttacks = 0;

        _startMemory = System.GC.GetTotalMemory(false);
        _peakMemory = _startMemory;

        _gcCollectionCount0 = System.GC.CollectionCount(0);
        _gcCollectionCount1 = System.GC.CollectionCount(1);
        _gcCollectionCount2 = System.GC.CollectionCount(2);

        _lastSampleTime = Time.unscaledTime;
        _simulationTimer.Restart();
        _isProfiling = true;

        Debug.Log($"[PerformanceProfiler] Started profiling {mode} mode");
    }

    public void StopProfiling()
    {
        if (!_isProfiling) return;

        _simulationTimer.Stop();
        _isProfiling = false;

        Debug.Log($"[PerformanceProfiler] Stopped profiling. Duration: {_simulationTimer.Elapsed.TotalSeconds:F2}s");

        if (_autoExportOnEnd)
        {
            ExportData();
        }
    }

    private void RecordSample()
    {
        var sample = new PerformanceSample
        {
            Timestamp = Time.unscaledTime,
            FPS = 1f / Time.unscaledDeltaTime,
            FrameTime = Time.unscaledDeltaTime * 1000f, // ms
            MemoryUsageMB = System.GC.GetTotalMemory(false) / (1024f * 1024f),
            ActiveUnits = GetActiveUnitCount(),
            GCCount0 = System.GC.CollectionCount(0) - _gcCollectionCount0,
            GCCount1 = System.GC.CollectionCount(1) - _gcCollectionCount1,
            GCCount2 = System.GC.CollectionCount(2) - _gcCollectionCount2
        };

        _samples.Add(sample);
    }

    private int GetActiveUnitCount()
    {
        if (GameStateManager.Instance != null)
        {
            return GameStateManager.Instance.ActiveTroops.Count;
        }
        return 0;
    }

    // Combat event tracking
    public void RecordDamage(int damage)
    {
        _totalDamageDealt += damage;
    }

    public void RecordUnitSpawned()
    {
        _totalUnitsSpawned++;
    }

    public void RecordUnitDeath()
    {
        _totalUnitsDied++;
    }

    public void RecordAttack()
    {
        _totalAttacks++;
    }

    public void ExportData()
    {
        if (_samples.Count == 0)
        {
            Debug.LogWarning("[PerformanceProfiler] No samples to export");
            return;
        }

        string directory = Path.Combine(Application.persistentDataPath, _exportPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename = $"Profile_{_currentMode}_{timestamp}.csv";
        string filepath = Path.Combine(directory, filename);

        ExportCSV(filepath);
        ExportSummary(Path.Combine(directory, $"Summary_{_currentMode}_{timestamp}.txt"));

        Debug.Log($"[PerformanceProfiler] Exported data to: {filepath}");
    }

    private void ExportCSV(string filepath)
    {
        StringBuilder csv = new StringBuilder();

        // Header
        csv.AppendLine("Timestamp,FPS,FrameTimeMs,MemoryMB,ActiveUnits,GC0,GC1,GC2");

        // Data rows
        foreach (var sample in _samples)
        {
            csv.AppendLine($"{sample.Timestamp:F3},{sample.FPS:F2},{sample.FrameTime:F3}," +
                          $"{sample.MemoryUsageMB:F2},{sample.ActiveUnits}," +
                          $"{sample.GCCount0},{sample.GCCount1},{sample.GCCount2}");
        }

        File.WriteAllText(filepath, csv.ToString());
    }

    private void ExportSummary(string filepath)
    {
        StringBuilder summary = new StringBuilder();

        summary.AppendLine("=== PERFORMANCE SUMMARY ===");
        summary.AppendLine($"Mode: {_currentMode}");
        summary.AppendLine($"Date: {System.DateTime.Now}");
        summary.AppendLine($"Duration: {_simulationTimer.Elapsed.TotalSeconds:F2} seconds");
        summary.AppendLine();

        // FPS Statistics
        var fpsSamples = _samples.Select(s => s.FPS).ToList();
        summary.AppendLine("--- FPS Statistics ---");
        summary.AppendLine($"Average FPS: {fpsSamples.Average():F2}");
        summary.AppendLine($"Min FPS: {fpsSamples.Min():F2}");
        summary.AppendLine($"Max FPS: {fpsSamples.Max():F2}");
        summary.AppendLine($"Median FPS: {GetMedian(fpsSamples):F2}");
        summary.AppendLine($"1% Low: {GetPercentile(fpsSamples, 0.01f):F2}");
        summary.AppendLine($"0.1% Low: {GetPercentile(fpsSamples, 0.001f):F2}");
        summary.AppendLine();

        // Frame Time Statistics
        var frameTimeSamples = _samples.Select(s => s.FrameTime).ToList();
        summary.AppendLine("--- Frame Time Statistics (ms) ---");
        summary.AppendLine($"Average: {frameTimeSamples.Average():F3}");
        summary.AppendLine($"Min: {frameTimeSamples.Min():F3}");
        summary.AppendLine($"Max: {frameTimeSamples.Max():F3}");
        summary.AppendLine($"99th Percentile: {GetPercentile(frameTimeSamples, 0.99f):F3}");
        summary.AppendLine();

        // Memory Statistics
        var memorySamples = _samples.Select(s => s.MemoryUsageMB).ToList();
        summary.AppendLine("--- Memory Statistics (MB) ---");
        summary.AppendLine($"Start: {_startMemory / (1024f * 1024f):F2}");
        summary.AppendLine($"Peak: {_peakMemory / (1024f * 1024f):F2}");
        summary.AppendLine($"Average: {memorySamples.Average():F2}");
        summary.AppendLine($"Total Allocated: {(_peakMemory - _startMemory) / (1024f * 1024f):F2}");
        summary.AppendLine();

        // GC Statistics
        summary.AppendLine("--- Garbage Collection ---");
        summary.AppendLine($"Gen 0 Collections: {_samples.LastOrDefault().GCCount0}");
        summary.AppendLine($"Gen 1 Collections: {_samples.LastOrDefault().GCCount1}");
        summary.AppendLine($"Gen 2 Collections: {_samples.LastOrDefault().GCCount2}");
        summary.AppendLine();

        // Combat Statistics
        summary.AppendLine("--- Combat Statistics ---");
        summary.AppendLine($"Total Units Spawned: {_totalUnitsSpawned}");
        summary.AppendLine($"Total Units Died: {_totalUnitsDied}");
        summary.AppendLine($"Total Attacks: {_totalAttacks}");
        summary.AppendLine($"Total Damage Dealt: {_totalDamageDealt}");
        if (_totalAttacks > 0)
        {
            summary.AppendLine($"Average Damage per Attack: {(float)_totalDamageDealt / _totalAttacks:F2}");
        }
        summary.AppendLine();

        // Frame Statistics
        summary.AppendLine("--- Frame Statistics ---");
        summary.AppendLine($"Total Frames: {_frameCount}");
        summary.AppendLine($"Average Frame Time: {(_deltaTimeSum / _frameCount) * 1000f:F3} ms");
        summary.AppendLine($"Min Frame Time: {_minFrameTime * 1000f:F3} ms");
        summary.AppendLine($"Max Frame Time: {_maxFrameTime * 1000f:F3} ms");

        File.WriteAllText(filepath, summary.ToString());
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
        if (!_showOnScreenStats || !_showStats || !_isProfiling) return;

        int yOffset = 10;
        int lineHeight = 25;
        int xOffset = 10;

        GUI.color = Color.white;
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.normal.textColor = Color.white;

        // Background
        GUI.Box(new Rect(5, 5, 350, 300), "");

        // Title
        GUI.Label(new Rect(xOffset, yOffset, 300, lineHeight),
                  $"<b>Performance Stats - {_currentMode}</b>", style);
        yOffset += lineHeight;

        // Current FPS
        float currentFPS = 1f / Time.unscaledDeltaTime;
        Color fpsColor = currentFPS >= 60 ? Color.green : currentFPS >= 30 ? Color.yellow : Color.red;
        style.normal.textColor = fpsColor;
        GUI.Label(new Rect(xOffset, yOffset, 300, lineHeight),
                  $"FPS: {currentFPS:F1}", style);
        yOffset += lineHeight;

        style.normal.textColor = Color.white;

        // Frame time
        GUI.Label(new Rect(xOffset, yOffset, 300, lineHeight),
                  $"Frame Time: {Time.unscaledDeltaTime * 1000f:F2} ms", style);
        yOffset += lineHeight;

        // Memory
        float memoryMB = System.GC.GetTotalMemory(false) / (1024f * 1024f);
        GUI.Label(new Rect(xOffset, yOffset, 300, lineHeight),
                  $"Memory: {memoryMB:F2} MB", style);
        yOffset += lineHeight;

        // Active units
        int activeUnits = GetActiveUnitCount();
        GUI.Label(new Rect(xOffset, yOffset, 300, lineHeight),
                  $"Active Units: {activeUnits}", style);
        yOffset += lineHeight;

        // Statistics (if samples available)
        if (_samples.Count > 0)
        {
            var recentSamples = _samples.TakeLast(100).ToList();
            float avgFPS = recentSamples.Select(s => s.FPS).Average();
            float minFPS = recentSamples.Select(s => s.FPS).Min();

            GUI.Label(new Rect(xOffset, yOffset, 300, lineHeight),
                      $"Avg FPS (100): {avgFPS:F1}", style);
            yOffset += lineHeight;

            GUI.Label(new Rect(xOffset, yOffset, 300, lineHeight),
                      $"Min FPS (100): {minFPS:F1}", style);
            yOffset += lineHeight;
        }

        // Combat stats
        GUI.Label(new Rect(xOffset, yOffset, 300, lineHeight),
                  $"Attacks: {_totalAttacks}", style);
        yOffset += lineHeight;

        GUI.Label(new Rect(xOffset, yOffset, 300, lineHeight),
                  $"Damage Dealt: {_totalDamageDealt}", style);
        yOffset += lineHeight;

        // Runtime
        GUI.Label(new Rect(xOffset, yOffset, 300, lineHeight),
                  $"Runtime: {_simulationTimer.Elapsed.TotalSeconds:F1}s", style);
        yOffset += lineHeight;

        // Toggle hint
        style.fontSize = 10;
        GUI.Label(new Rect(xOffset, yOffset, 300, lineHeight),
                  $"Press {_toggleStatsKey} to toggle", style);
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
}