using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Tool for comparing performance between multiple simulation runs
/// </summary>
public class PerformanceComparison : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject _comparisonPanel;
    [SerializeField] private TextMeshProUGUI _comparisonText;
    [SerializeField] private Button _loadUnityDataButton;
    [SerializeField] private Button _loadAtomsDataButton;
    [SerializeField] private Button _compareButton;
    [SerializeField] private Button _exportComparisonButton;

    private SimulationData _unityData;
    private SimulationData _atomsData;

    void Start()
    {
        if (_loadUnityDataButton != null)
            _loadUnityDataButton.onClick.AddListener(() => LoadLatestData(SimulationMode.Unity));

        if (_loadAtomsDataButton != null)
            _loadAtomsDataButton.onClick.AddListener(() => LoadLatestData(SimulationMode.Atoms));

        if (_compareButton != null)
            _compareButton.onClick.AddListener(CompareData);

        if (_exportComparisonButton != null)
            _exportComparisonButton.onClick.AddListener(ExportComparison);
    }

    private void LoadLatestData(SimulationMode mode)
    {
        string directory = Path.Combine(Application.persistentDataPath, "PerformanceData");
        if (!Directory.Exists(directory))
        {
            Debug.LogWarning($"[PerformanceComparison] Directory not found: {directory}");
            return;
        }

        var summaryFiles = Directory.GetFiles(directory, $"Summary_{mode}_*.txt")
                                   .OrderByDescending(f => File.GetCreationTime(f))
                                   .ToList();

        if (summaryFiles.Count == 0)
        {
            Debug.LogWarning($"[PerformanceComparison] No data found for {mode}");
            return;
        }

        string latestFile = summaryFiles[0];
        var data = ParseSummaryFile(latestFile, mode);

        if (mode == SimulationMode.Unity)
            _unityData = data;
        else
            _atomsData = data;

        Debug.Log($"[PerformanceComparison] Loaded {mode} data from {Path.GetFileName(latestFile)}");
    }

    private SimulationData ParseSummaryFile(string filepath, SimulationMode mode)
    {
        var data = new SimulationData { Mode = mode };
        string[] lines = File.ReadAllLines(filepath);

        foreach (string line in lines)
        {
            if (line.Contains("Average FPS:"))
                data.AvgFPS = ParseFloat(line);
            else if (line.Contains("Min FPS:"))
                data.MinFPS = ParseFloat(line);
            else if (line.Contains("1% Low:"))
                data.OnePercentLow = ParseFloat(line);
            else if (line.Contains("Average:") && line.Contains("Frame Time"))
                data.AvgFrameTime = ParseFloat(line);
            else if (line.Contains("Peak:") && line.Contains("Memory"))
                data.PeakMemory = ParseFloat(line);
            else if (line.Contains("Gen 0 Collections:"))
                data.GC0 = ParseInt(line);
            else if (line.Contains("Gen 1 Collections:"))
                data.GC1 = ParseInt(line);
            else if (line.Contains("Total Attacks:"))
                data.TotalAttacks = ParseInt(line);
            else if (line.Contains("Duration:"))
                data.Duration = ParseFloat(line);
        }

        return data;
    }

    private float ParseFloat(string line)
    {
        var parts = line.Split(':');
        if (parts.Length < 2) return 0f;

        string valueStr = parts[1].Trim().Split(' ')[0];
        float.TryParse(valueStr, out float value);
        return value;
    }

    private int ParseInt(string line)
    {
        var parts = line.Split(':');
        if (parts.Length < 2) return 0;

        string valueStr = parts[1].Trim().Split(' ')[0];
        int.TryParse(valueStr, out int value);
        return value;
    }

    private void CompareData()
    {
        if (_unityData == null || _atomsData == null)
        {
            Debug.LogWarning("[PerformanceComparison] Need both Unity and Atoms data");
            return;
        }

        System.Text.StringBuilder comparison = new System.Text.StringBuilder();
        comparison.AppendLine("=== PERFORMANCE COMPARISON ===\n");

        // FPS Comparison
        comparison.AppendLine("--- FPS ---");
        comparison.AppendLine($"Unity Avg: {_unityData.AvgFPS:F2}");
        comparison.AppendLine($"Atoms Avg: {_atomsData.AvgFPS:F2}");
        float fpsDiff = ((_atomsData.AvgFPS - _unityData.AvgFPS) / _unityData.AvgFPS) * 100f;
        comparison.AppendLine($"Difference: {fpsDiff:+0.00;-0.00}%");
        comparison.AppendLine($"Winner: {(fpsDiff > 0 ? "Atoms" : "Unity")}\n");

        // Frame Time Comparison
        comparison.AppendLine("--- Frame Time ---");
        comparison.AppendLine($"Unity Avg: {_unityData.AvgFrameTime:F3} ms");
        comparison.AppendLine($"Atoms Avg: {_atomsData.AvgFrameTime:F3} ms");
        float ftDiff = ((_unityData.AvgFrameTime - _atomsData.AvgFrameTime) / _unityData.AvgFrameTime) * 100f;
        comparison.AppendLine($"Improvement: {ftDiff:+0.00;-0.00}%");
        comparison.AppendLine($"Winner: {(ftDiff > 0 ? "Atoms" : "Unity")}\n");

        // Memory Comparison
        comparison.AppendLine("--- Memory ---");
        comparison.AppendLine($"Unity Peak: {_unityData.PeakMemory:F2} MB");
        comparison.AppendLine($"Atoms Peak: {_atomsData.PeakMemory:F2} MB");
        float memDiff = ((_unityData.PeakMemory - _atomsData.PeakMemory) / _unityData.PeakMemory) * 100f;
        comparison.AppendLine($"Reduction: {memDiff:+0.00;-0.00}%");
        comparison.AppendLine($"Winner: {(memDiff > 0 ? "Atoms" : "Unity")}\n");

        // GC Comparison
        comparison.AppendLine("--- Garbage Collection ---");
        comparison.AppendLine($"Unity Gen0: {_unityData.GC0}");
        comparison.AppendLine($"Atoms Gen0: {_atomsData.GC0}");
        int gcDiff = _unityData.GC0 - _atomsData.GC0;
        comparison.AppendLine($"Reduction: {gcDiff}");
        comparison.AppendLine($"Winner: {(gcDiff > 0 ? "Atoms" : "Unity")}\n");

        // Summary
        comparison.AppendLine("--- SUMMARY ---");
        int atomsWins = 0;
        int unityWins = 0;

        if (fpsDiff > 0) atomsWins++; else unityWins++;
        if (ftDiff > 0) atomsWins++; else unityWins++;
        if (memDiff > 0) atomsWins++; else unityWins++;
        if (gcDiff > 0) atomsWins++; else unityWins++;

        comparison.AppendLine($"Unity Wins: {unityWins}");
        comparison.AppendLine($"Atoms Wins: {atomsWins}");
        comparison.AppendLine($"Overall Winner: {(atomsWins > unityWins ? "Atoms" : "Unity")}");

        if (_comparisonText != null)
        {
            _comparisonText.text = comparison.ToString();
        }

        Debug.Log(comparison.ToString());
    }

    private void ExportComparison()
    {
        if (_unityData == null || _atomsData == null) return;

        string directory = Path.Combine(Application.persistentDataPath, "PerformanceData");
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filepath = Path.Combine(directory, $"Comparison_{timestamp}.txt");

        File.WriteAllText(filepath, _comparisonText.text);
        Debug.Log($"[PerformanceComparison] Exported comparison to: {filepath}");
    }

    [System.Serializable]
    private class SimulationData
    {
        public SimulationMode Mode;
        public float AvgFPS;
        public float MinFPS;
        public float OnePercentLow;
        public float AvgFrameTime;
        public float PeakMemory;
        public int GC0;
        public int GC1;
        public int TotalAttacks;
        public float Duration;
    }
}