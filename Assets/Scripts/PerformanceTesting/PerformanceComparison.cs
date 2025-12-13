using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Udar.SceneManager;
using System;

/// <summary>
/// Enhanced Performance Comparison scene manager with deep Atoms analysis
/// Loads, compares, and displays comprehensive performance data
/// </summary>
public class PerformanceComparison : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject _mainPanel;
    [SerializeField] private Button _backToMenuButton;

    [Header("Data Loading")]
    [SerializeField] private Button _loadUnityButton;
    [SerializeField] private Button _loadAtomsButton;
    [SerializeField] private GameObject _fileSelectionPanel;
    [SerializeField] private Transform _fileListContainer;
    [SerializeField] private GameObject _fileButtonPrefab;
    [SerializeField] private TextMeshProUGUI _unityDataStatusText;
    [SerializeField] private TextMeshProUGUI _atomsDataStatusText;

    [Header("Comparison")]
    [SerializeField] private Button _compareButton;
    [SerializeField] private Button _exportComparisonButton;
    [SerializeField] private Button _copyToClipboardButton;
    [SerializeField] private ScrollRect _comparisonScrollView;
    [SerializeField] private TextMeshProUGUI _comparisonText;
    [SerializeField] private TextMeshProUGUI _clipboardSuccessText;

    [Header("Phase Tabs")]
    [SerializeField] private Button _allPhasesButton;
    [SerializeField] private Button _prepPhaseButton;
    [SerializeField] private Button _simulatePhaseButton;
    [SerializeField] private Button _winPhaseButton;

    [Header("Analysis Options")]
    [SerializeField] private Toggle _showDeepAnalysisToggle;
    [SerializeField] private Toggle _showWarmupDataToggle;
    [SerializeField] private Toggle _showAtomsMetricsToggle;

    [Header("Scene Settings")]
    [SerializeField] private SceneField _mainMenuScene;

    [Header("File Path Display")]
    [SerializeField] private TextMeshProUGUI _savePathText;

    private SessionData _unityData;
    private SessionData _atomsData;
    private SimulationMode _currentLoadingMode;
    private PhaseFilter _currentPhaseFilter = PhaseFilter.All;
    private ComparisonResult _lastComparison;

    private bool _showDeepAnalysis = true;
    private bool _showWarmupData = true;
    private bool _showAtomsMetrics = true;

    private enum PhaseFilter
    {
        All,
        Prep,
        Simulate,
        Win
    }

    void Start()
    {
        SetupButtons();
        SetupToggles();
        UpdateUI();
        
        // Hide clipboard success text initially
        if (_clipboardSuccessText != null)
            _clipboardSuccessText.gameObject.SetActive(false);
        
        // ADDED: Display save path
        DisplaySavePath();
    }

    private void SetupButtons()
    {
        if (_backToMenuButton != null)
            _backToMenuButton.onClick.AddListener(OnBackToMenuClicked);

        if (_loadUnityButton != null)
            _loadUnityButton.onClick.AddListener(() => OnLoadDataClicked(SimulationMode.Unity));

        if (_loadAtomsButton != null)
            _loadAtomsButton.onClick.AddListener(() => OnLoadDataClicked(SimulationMode.Atoms));

        if (_compareButton != null)
            _compareButton.onClick.AddListener(OnCompareClicked);

        if (_exportComparisonButton != null)
            _exportComparisonButton.onClick.AddListener(OnExportComparisonClicked);

        // ADDED: Copy to clipboard button
        if (_copyToClipboardButton != null)
            _copyToClipboardButton.onClick.AddListener(OnCopyToClipboardClicked);

        // Phase filter buttons
        if (_allPhasesButton != null)
            _allPhasesButton.onClick.AddListener(() => OnPhaseFilterChanged(PhaseFilter.All));

        if (_prepPhaseButton != null)
            _prepPhaseButton.onClick.AddListener(() => OnPhaseFilterChanged(PhaseFilter.Prep));

        if (_simulatePhaseButton != null)
            _simulatePhaseButton.onClick.AddListener(() => OnPhaseFilterChanged(PhaseFilter.Simulate));

        if (_winPhaseButton != null)
            _winPhaseButton.onClick.AddListener(() => OnPhaseFilterChanged(PhaseFilter.Win));
    }

    private void SetupToggles()
    {
        if (_showDeepAnalysisToggle != null)
        {
            _showDeepAnalysisToggle.isOn = _showDeepAnalysis;
            _showDeepAnalysisToggle.onValueChanged.AddListener(value =>
            {
                _showDeepAnalysis = value;
                if (_lastComparison != null) OnCompareClicked();
            });
        }

        if (_showWarmupDataToggle != null)
        {
            _showWarmupDataToggle.isOn = _showWarmupData;
            _showWarmupDataToggle.onValueChanged.AddListener(value =>
            {
                _showWarmupData = value;
                if (_lastComparison != null) OnCompareClicked();
            });
        }

        if (_showAtomsMetricsToggle != null)
        {
            _showAtomsMetricsToggle.isOn = _showAtomsMetrics;
            _showAtomsMetricsToggle.onValueChanged.AddListener(value =>
            {
                _showAtomsMetrics = value;
                if (_lastComparison != null) OnCompareClicked();
            });
        }
    }

    private void UpdateUI()
    {
        // Update status texts
        if (_unityDataStatusText != null)
        {
            _unityDataStatusText.text = _unityData != null
                ? $"✓ Loaded: {_unityData.SessionId}"
                : "No data loaded";
            _unityDataStatusText.color = _unityData != null ? Color.green : Color.gray;
        }

        if (_atomsDataStatusText != null)
        {
            _atomsDataStatusText.text = _atomsData != null
                ? $"✓ Loaded: {_atomsData.SessionId}"
                : "No data loaded";
            _atomsDataStatusText.color = _atomsData != null ? Color.green : Color.gray;
        }

        // Enable/disable buttons based on state
        bool hasComparison = _lastComparison != null;
        
        if (_compareButton != null)
            _compareButton.interactable = _unityData != null && _atomsData != null;

        if (_exportComparisonButton != null)
            _exportComparisonButton.interactable = hasComparison;

        if (_copyToClipboardButton != null)
            _copyToClipboardButton.interactable = hasComparison;

        // Update phase button colors
        UpdatePhaseButtonColors();
    }

    private void UpdatePhaseButtonColors()
    {
        Color activeColor = new Color(0.3f, 0.6f, 1f);
        Color inactiveColor = Color.white;

        SetButtonColor(_allPhasesButton, _currentPhaseFilter == PhaseFilter.All ? activeColor : inactiveColor);
        SetButtonColor(_prepPhaseButton, _currentPhaseFilter == PhaseFilter.Prep ? activeColor : inactiveColor);
        SetButtonColor(_simulatePhaseButton, _currentPhaseFilter == PhaseFilter.Simulate ? activeColor : inactiveColor);
        SetButtonColor(_winPhaseButton, _currentPhaseFilter == PhaseFilter.Win ? activeColor : inactiveColor);
    }

    private void SetButtonColor(Button button, Color color)
    {
        if (button != null)
        {
            var colors = button.colors;
            colors.normalColor = color;
            button.colors = colors;
        }
    }

    private void OnBackToMenuClicked()
    {
        Debug.Log("[PerformanceComparison] Returning to main menu");
        UnityEngine.SceneManagement.SceneManager.LoadScene(_mainMenuScene.Name);
    }

    private void OnLoadDataClicked(SimulationMode mode)
    {
        _currentLoadingMode = mode;
        ShowFileSelectionPanel(mode);
    }

    private void ShowFileSelectionPanel(SimulationMode mode)
    {
        if (_fileSelectionPanel == null || _fileListContainer == null || _fileButtonPrefab == null)
        {
            Debug.LogError("[PerformanceComparison] Missing file selection UI references");
            return;
        }

        // Clear existing buttons
        foreach (Transform child in _fileListContainer)
        {
            Destroy(child.gameObject);
        }

        // Get available data files
        string directory = Path.Combine(Application.persistentDataPath, "PerformanceData");
        if (!Directory.Exists(directory))
        {
            Debug.LogWarning("[PerformanceComparison] No performance data directory found");
            CreateNoFilesMessage(mode);
            _fileSelectionPanel.SetActive(true);
            return;
        }

        // ✅ FIXED: Updated search pattern to match new naming convention
        // OLD: $"{mode}_*_Data.json"  (matches: Unity_20240101_120000_Data.json)
        // NEW: $"({mode})DataFile_*_Data.json" (matches: (Unity)DataFile_01-01-24_12-00-00_Data.json)
        var jsonFiles = Directory.GetFiles(directory, $"({mode})DataFile_*_Data.json")
                             .OrderByDescending(f => File.GetCreationTime(f))
                             .ToList();

        if (jsonFiles.Count == 0)
        {
            Debug.LogWarning($"[PerformanceComparison] No {mode} data files found");
            
            // ✅ Also try old naming pattern for backward compatibility
            var oldPatternFiles = Directory.GetFiles(directory, $"{mode}_*_Data.json")
                                       .OrderByDescending(f => File.GetCreationTime(f))
                                       .ToList();
            
            if (oldPatternFiles.Count > 0)
            {
                Debug.Log($"[PerformanceComparison] Found {oldPatternFiles.Count} files with old naming pattern");
                jsonFiles = oldPatternFiles;
            }
            else
            {
                CreateNoFilesMessage(mode);
                _fileSelectionPanel.SetActive(true);
                return;
            }
        }

        // Create button for EACH file (show all, not just one)
        foreach (string filePath in jsonFiles)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            
            // ✅ FIXED: Parse both old and new filename formats
            string displayName;
            
            // Check if new format: (Mode)DataFile_DD-MM-YY_HH-mm-SS_Data
            if (fileName.StartsWith($"({mode})DataFile_"))
            {
                // New format - extract date from filename
                string dateTimePart = fileName.Replace($"({mode})DataFile_", "").Replace("_Data", "");
                displayName = $"({mode})DataFile_{dateTimePart}";
            }
            else
            {
                // Old format - use file creation time
                string formattedDate = fileInfo.CreationTime.ToString("dd/MM/yy HH:mm:ss");
                displayName = $"({mode})DataFile_{formattedDate}";
            }
            
            // Add file size
            string fileSize = FormatFileSize(fileInfo.Length);
            displayName += $" - {fileSize}";

            GameObject buttonObj = Instantiate(_fileButtonPrefab, _fileListContainer);
            
            // Setup main button text
            var text = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = displayName;
            }

            // Find buttons (assuming prefab has Load and Delete buttons)
            Button[] buttons = buttonObj.GetComponentsInChildren<Button>();
            
            if (buttons.Length >= 1)
            {
                // First button is Load
                Button loadButton = buttons[0];
                string capturedPath = filePath;
                loadButton.onClick.AddListener(() => OnFileSelected(capturedPath));
            }

            if (buttons.Length >= 2)
            {
                // Second button is Delete
                Button deleteButton = buttons[1];
                string capturedPath = filePath;
                string capturedDisplayName = displayName;
                deleteButton.onClick.AddListener(() => OnDeleteFileClicked(capturedPath, capturedDisplayName));
            }
        }

        _fileSelectionPanel.SetActive(true);
    }

    // Add helper method for file size formatting
    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        
        return $"{len:F2} {sizes[order]}";
    }

    private void CreateNoFilesMessage(SimulationMode mode)
    {
        GameObject noFilesObj = Instantiate(_fileButtonPrefab, _fileListContainer);
        var text = noFilesObj.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
            text.text = $"No {mode} data files found";
        
        // Disable all buttons
        var buttons = noFilesObj.GetComponentsInChildren<Button>();
        foreach (var btn in buttons)
            btn.interactable = false;
    }

    private void OnFileSelected(string filePath)
    {
        Debug.Log($"[PerformanceComparison] Loading: {filePath}");

        try
        {
            string json = File.ReadAllText(filePath);
            var sessionData = JsonUtility.FromJson<SessionData>(json);

            if (_currentLoadingMode == SimulationMode.Unity)
            {
                _unityData = sessionData;
            }
            else
            {
                _atomsData = sessionData;
            }

            Debug.Log($"[PerformanceComparison] Successfully loaded {_currentLoadingMode} data");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PerformanceComparison] Failed to load data: {e.Message}");
        }

        _fileSelectionPanel.SetActive(false);
        UpdateUI();
    }

    private void OnDeleteFileClicked(string filePath, string displayName)
    {
        Debug.Log($"[PerformanceComparison] Deleting: {filePath}");

        try
        {
            // Delete the main JSON file
            if (File.Exists(filePath))
                File.Delete(filePath);

            // ✅ FIXED: Handle both old and new filename formats
            string directory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            
            // Remove the "_Data" suffix to get the base name
            string filePrefix = fileName.Replace("_Data", "");
            
            Debug.Log($"[PerformanceComparison] Looking for related files: {filePrefix}*");
            
            // Delete all related files (Summary, CSVs, etc.)
            var relatedFiles = Directory.GetFiles(directory, $"{filePrefix}*");
            
            foreach (var file in relatedFiles)
            {
                if (File.Exists(file))
                {
                    Debug.Log($"[PerformanceComparison] Deleting related file: {Path.GetFileName(file)}");
                    File.Delete(file);
                }
            }

            Debug.Log($"[PerformanceComparison] Deleted {displayName} and {relatedFiles.Length} associated files");

            // Refresh the file list
            ShowFileSelectionPanel(_currentLoadingMode);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PerformanceComparison] Failed to delete file: {e.Message}\n{e.StackTrace}");
        }
    }

    private void OnPhaseFilterChanged(PhaseFilter filter)
    {
        _currentPhaseFilter = filter;
        UpdateUI();

        // Re-run comparison if we have data
        if (_lastComparison != null)
        {
            OnCompareClicked();
        }
    }

    private void OnCompareClicked()
    {
        if (_unityData == null || _atomsData == null)
        {
            Debug.LogWarning("[PerformanceComparison] Need both Unity and Atoms data to compare");
            return;
        }

        Debug.Log("[PerformanceComparison] Comparing data...");

        _lastComparison = CompareData(_unityData, _atomsData, _currentPhaseFilter);
        DisplayComparison(_lastComparison);
        UpdateUI();
    }

    private ComparisonResult CompareData(SessionData unity, SessionData atoms, PhaseFilter filter)
    {
        var result = new ComparisonResult
        {
            UnitySessionId = unity.SessionId,
            AtomsSessionId = atoms.SessionId,
            PhaseComparisons = new List<PhaseComparison>()
        };

        var phasesToCompare = GetPhasesToCompare(unity, atoms, filter);

        foreach (var phasePair in phasesToCompare)
        {
            var unityPhase = phasePair.Item1;
            var atomsPhase = phasePair.Item2;

            var phaseComp = new PhaseComparison
            {
                PhaseName = unityPhase.Phase,

                // Basic Metrics (unchanged)
                UnityAvgFPS = unityPhase.AvgFPS,
                AtomsAvgFPS = atomsPhase.AvgFPS,
                FPSDifference = atomsPhase.AvgFPS - unityPhase.AvgFPS,
                FPSPercentChange = CalculatePercentChange(unityPhase.AvgFPS, atomsPhase.AvgFPS),

                UnityMinFPS = unityPhase.MinFPS,
                AtomsMinFPS = atomsPhase.MinFPS,

                UnityAvgFrameTime = unityPhase.AvgFrameTime,
                AtomsAvgFrameTime = atomsPhase.AvgFrameTime,
                FrameTimeDifference = unityPhase.AvgFrameTime - atomsPhase.AvgFrameTime,
                FrameTimePercentChange = CalculatePercentChange(unityPhase.AvgFrameTime, atomsPhase.AvgFrameTime),

                UnityPeakMemory = unityPhase.PeakMemoryMB,
                AtomsPeakMemory = atomsPhase.PeakMemoryMB,
                MemoryDifference = unityPhase.PeakMemoryMB - atomsPhase.PeakMemoryMB,
                MemoryPercentChange = CalculatePercentChange(unityPhase.PeakMemoryMB, atomsPhase.PeakMemoryMB),

                UnityGC0 = unityPhase.GC0,
                AtomsGC0 = atomsPhase.GC0,
                GC0Difference = unityPhase.GC0 - atomsPhase.GC0,

                UnityGC1 = unityPhase.GC1,
                AtomsGC1 = atomsPhase.GC1,

                UnityGC2 = unityPhase.GC2,
                AtomsGC2 = atomsPhase.GC2,

                UnityAttacks = unityPhase.TotalAttacks,
                AtomsAttacks = atomsPhase.TotalAttacks,
                UnityDamage = unityPhase.TotalDamage,
                AtomsDamage = atomsPhase.TotalDamage,

                // Enhanced Metrics (unchanged)
                UnityMainThreadTime = unityPhase.AvgMainThreadTime,
                AtomsMainThreadTime = atomsPhase.AvgMainThreadTime,
                MainThreadTimeDiff = unityPhase.AvgMainThreadTime - atomsPhase.AvgMainThreadTime,

                UnityTotalGCAlloc = unityPhase.TotalGCAlloc,
                AtomsTotalGCAlloc = atomsPhase.TotalGCAlloc,
                GCAllocDiff = (long)(unityPhase.TotalGCAlloc - atomsPhase.TotalGCAlloc),
                
                // ADDED: Shared metrics
                UnityProjectilesSpawned = unityPhase.ProjectilesSpawned,
                AtomsProjectilesSpawned = atomsPhase.ProjectilesSpawned,
                UnityProjectileRetargets = unityPhase.ProjectileRetargets,
                AtomsProjectileRetargets = atomsPhase.ProjectileRetargets,
                UnitySplashDamageHits = unityPhase.SplashDamageHits,
                AtomsSplashDamageHits = atomsPhase.SplashDamageHits,
                UnityTotalSplashDamage = unityPhase.TotalSplashDamage,
                AtomsTotalSplashDamage = atomsPhase.TotalSplashDamage,
                
                UnityNavMeshRecalculations = unityPhase.NavMeshRecalculations,
                AtomsNavMeshRecalculations = atomsPhase.NavMeshRecalculations,
                UnityNavMeshStucks = unityPhase.NavMeshStucks,
                AtomsNavMeshStucks = atomsPhase.NavMeshStucks,
                UnityTotalPathLength = unityPhase.TotalPathLength,
                AtomsTotalPathLength = atomsPhase.TotalPathLength,
                
                UnityStateTransitions = unityPhase.StateTransitions,
                AtomsStateTransitions = atomsPhase.StateTransitions,
                
                UnityAnimationTriggers = unityPhase.AnimationTriggers,
                AtomsAnimationTriggers = atomsPhase.AnimationTriggers
            };

            result.PhaseComparisons.Add(phaseComp);
        }

        result.CalculateWinner();
        return result;
    }

    private List<(PhaseData, PhaseData)> GetPhasesToCompare(SessionData unity, SessionData atoms, PhaseFilter filter)
    {
        var pairs = new List<(PhaseData, PhaseData)>();

        if (filter == PhaseFilter.All)
        {
            // Compare all matching phases
            foreach (var unityPhase in unity.Phases)
            {
                var atomsPhase = atoms.Phases.FirstOrDefault(p => p.Phase == unityPhase.Phase);
                if (atomsPhase != null)
                {
                    pairs.Add((unityPhase, atomsPhase));
                }
            }
        }
        else
        {
            // Compare specific phase
            string phaseName = filter.ToString();
            var unityPhase = unity.Phases.FirstOrDefault(p => p.Phase == phaseName);
            var atomsPhase = atoms.Phases.FirstOrDefault(p => p.Phase == phaseName);

            if (unityPhase != null && atomsPhase != null)
            {
                pairs.Add((unityPhase, atomsPhase));
            }
        }

        return pairs;
    }

    private float CalculatePercentChange(float baseline, float comparison)
    {
        if (baseline == 0f) return 0f;
        return ((comparison - baseline) / baseline) * 100f;
    }

    private void DisplayComparison(ComparisonResult result)
    {
        if (_comparisonText == null) return;

        var sb = new System.Text.StringBuilder();

        sb.AppendLine("╔══════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║              PERFORMANCE COMPARISON RESULTS                      ║");
        sb.AppendLine("╚══════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();
        sb.AppendLine($"<b>Unity Session:</b> {result.UnitySessionId}");
        sb.AppendLine($"<b>Atoms Session:</b> {result.AtomsSessionId}");
        sb.AppendLine($"<b>Filter:</b> {_currentPhaseFilter}");
        sb.AppendLine();

        // Warm-up comparison (moved to Atoms section as requested)

        foreach (var phase in result.PhaseComparisons)
        {
            sb.AppendLine($"┌────────────────────────────────────────────────────────────────┐");
            sb.AppendLine($"│ <b>PHASE: {phase.PhaseName,-56}</b> │");
            sb.AppendLine($"└────────────────────────────────────────────────────────────────┘");
            sb.AppendLine();

            // FPS Comparison
            sb.AppendLine("<b>─── FPS Performance ───</b>");
            sb.AppendLine($"  Unity Avg:    {phase.UnityAvgFPS:F2}");
            sb.AppendLine($"  Atoms Avg:    {phase.AtomsAvgFPS:F2}");
            sb.AppendLine($"  Difference:   {FormatDifference(phase.FPSDifference, true)} ({FormatPercent(phase.FPSPercentChange, true)})");
            sb.AppendLine($"  Unity Min:    {phase.UnityMinFPS:F2}");
            sb.AppendLine($"  Atoms Min:    {phase.AtomsMinFPS:F2}");
            sb.AppendLine($"  <color={(phase.FPSDifference > 0 ? "green" : "orange")}>Winner: {(phase.FPSDifference > 0 ? "Atoms" : "Unity")}</color>");
            sb.AppendLine();

            // Frame Time Comparison
            sb.AppendLine("<b>─── Frame Time (ms) ───</b>");
            sb.AppendLine($"  Unity Avg:    {phase.UnityAvgFrameTime:F3}");
            sb.AppendLine($"  Atoms Avg:    {phase.AtomsAvgFrameTime:F3}");
            sb.AppendLine($"  Improvement:  {FormatDifference(phase.FrameTimeDifference, false)} ({FormatPercent(phase.FrameTimePercentChange, false)})");
            sb.AppendLine($"  <color={(phase.FrameTimeDifference > 0 ? "green" : "orange")}>Winner: {(phase.FrameTimeDifference > 0 ? "Atoms" : "Unity")}</color>");
            sb.AppendLine();

            // Enhanced: Main Thread Time
            if (_showDeepAnalysis)
            {
                sb.AppendLine("<b>─── Main Thread Time (ms) ───</b>");
                sb.AppendLine($"  Unity Avg:    {phase.UnityMainThreadTime:F3}");
                sb.AppendLine($"  Atoms Avg:    {phase.AtomsMainThreadTime:F3}");
                sb.AppendLine($"  Difference:   {FormatDifference(phase.MainThreadTimeDiff, false)}");
                sb.AppendLine($"  <color={(phase.MainThreadTimeDiff > 0 ? "green" : "orange")}>Winner: {(phase.MainThreadTimeDiff > 0 ? "Atoms" : "Unity")}</color>");
                sb.AppendLine();
            }

            // Memory Comparison
            sb.AppendLine("<b>─── Memory Usage (MB) ───</b>");
            sb.AppendLine($"  Unity Peak:   {phase.UnityPeakMemory:F2}");
            sb.AppendLine($"  Atoms Peak:   {phase.AtomsPeakMemory:F2}");
            sb.AppendLine($"  Reduction:    {FormatDifference(phase.MemoryDifference, false)} ({FormatPercent(phase.MemoryPercentChange, false)})");
            sb.AppendLine($"  <color={(phase.MemoryDifference > 0 ? "green" : "orange")}>Winner: {(phase.MemoryDifference > 0 ? "Atoms" : "Unity")}</color>");
            sb.AppendLine();

            // GC Comparison
            sb.AppendLine("<b>─── Garbage Collection ───</b>");
            sb.AppendLine($"  Unity Gen0:   {phase.UnityGC0}");
            sb.AppendLine($"  Atoms Gen0:   {phase.AtomsGC0}");
            sb.AppendLine($"  Reduction:    {FormatDifference(phase.GC0Difference, false)}");
            sb.AppendLine($"  Unity Gen1:   {phase.UnityGC1}");
            sb.AppendLine($"  Atoms Gen1:   {phase.AtomsGC1}");
            sb.AppendLine($"  Unity Gen2:   {phase.UnityGC2}");
            sb.AppendLine($"  Atoms Gen2:   {phase.AtomsGC2}");
            
            if (_showDeepAnalysis)
            {
                sb.AppendLine($"  Unity Total GC Alloc: {FormatBytes(phase.UnityTotalGCAlloc)}");
                sb.AppendLine($"  Atoms Total GC Alloc: {FormatBytes(phase.AtomsTotalGCAlloc)}");
                sb.AppendLine($"  Difference: {FormatBytes(phase.GCAllocDiff)}");
            }
            
            sb.AppendLine($"  <color={(phase.GC0Difference > 0 ? "green" : "orange")}>Winner: {(phase.GC0Difference > 0 ? "Atoms" : "Unity")}</color>");
            sb.AppendLine();

            // Combat stats (Simulate phase only)
            if (phase.PhaseName == "Simulate")
            {
                sb.AppendLine("<b>─── Combat Verification ───</b>");
                sb.AppendLine($"  Unity Attacks: {phase.UnityAttacks}");
                sb.AppendLine($"  Atoms Attacks: {phase.AtomsAttacks}");
                sb.AppendLine($"  Unity Damage:  {phase.UnityDamage}");
                sb.AppendLine($"  Atoms Damage:  {phase.AtomsDamage}");
                bool combatMatch = phase.UnityAttacks == phase.AtomsAttacks && phase.UnityDamage == phase.AtomsDamage;
                sb.AppendLine($"  <color={(combatMatch ? "green" : "red")}>Combat Logic: {(combatMatch ? "✓ IDENTICAL" : "✗ DIFFERS")}</color>");
                sb.AppendLine();
            }

            sb.AppendLine();
        }

        // ============================================================================
        // ATOMS-SPECIFIC DEEP ANALYSIS - ALL CATEGORIES
        // ============================================================================
        if (_showAtomsMetrics && _atomsData.AtomsMetrics != null)
        {
            sb.AppendLine("╔══════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║          ATOMS-SPECIFIC DEEP ANALYSIS (ALL CATEGORIES)          ║");
            sb.AppendLine("╚══════════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            
            var metrics = _atomsData.AtomsMetrics;

            // MOVED: Warm-up Phase Analysis (Atoms-only overhead)
            if (_showWarmupData && metrics.WarmupDuration > 0)
            {
                sb.AppendLine("┌────────────────────────────────────────────────────────────────┐");
                sb.AppendLine("│ <b>0. WARM-UP PHASE OVERHEAD</b>                                    │");
                sb.AppendLine("└────────────────────────────────────────────────────────────────┘");
                sb.AppendLine($"  Duration: {metrics.WarmupDuration:F2}s");
                sb.AppendLine($"  Memory Allocated: {metrics.WarmupMemoryAlloc:F2} MB");
                sb.AppendLine($"  <color=yellow>Unity Equivalent: No warm-up overhead (immediate start)</color>");
                sb.AppendLine();
            }

            // 1. Event System Runtime Cost
            sb.AppendLine("┌────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("│ <b>1. EVENT SYSTEM RUNTIME COST</b>                                 │");
            sb.AppendLine("└────────────────────────────────────────────────────────────────┘");
            sb.AppendLine($"  Total Event Dispatches: {metrics.EventDispatches:N0}");
            sb.AppendLine($"  Total Listener Invocations: {metrics.EventListenerInvocations:N0}");
            
            if (metrics.EventDispatches > 0)
            {
                sb.AppendLine($"  Avg Listeners/Event: {(float)metrics.EventListenerInvocations / metrics.EventDispatches:F2}");
            }
            
            if (metrics.AvgEventDispatchTime > 0)
            {
                sb.AppendLine($"  Avg Dispatch Time: {metrics.AvgEventDispatchTime:F3} μs");
                sb.AppendLine($"  Total Event Overhead: {(metrics.AvgEventDispatchTime * metrics.EventDispatches) / 1000f:F2} ms");
            }
            
            sb.AppendLine($"  <color=yellow>Unity Equivalent: Direct method calls (near-zero overhead)</color>");
            sb.AppendLine();

            // 2. Variable Access & Mutation Cost
            sb.AppendLine("┌────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("│ <b>2. VARIABLE ACCESS & MUTATION COST</b>                           │");
            sb.AppendLine("└────────────────────────────────────────────────────────────────┘");
            sb.AppendLine($"  Variable Reads: {metrics.VariableReads:N0}");
            sb.AppendLine($"  Variable Writes: {metrics.VariableWrites:N0}");
            sb.AppendLine($"  Read/Write Ratio: {(metrics.VariableReads > 0 ? (float)metrics.VariableReads / metrics.VariableWrites : 0):F2}:1");
            
            if (metrics.AvgVariableReadTime > 0)
            {
                sb.AppendLine($"  Avg Read Time: {metrics.AvgVariableReadTime:F3} μs");
                sb.AppendLine($"  Total Read Overhead: {(metrics.AvgVariableReadTime * metrics.VariableReads) / 1000f:F2} ms");
            }
            
            if (metrics.AvgVariableWriteTime > 0)
            {
                sb.AppendLine($"  Avg Write Time: {metrics.AvgVariableWriteTime:F3} μs");
                sb.AppendLine($"  Total Write Overhead: {(metrics.AvgVariableWriteTime * metrics.VariableWrites) / 1000f:F2} ms");
            }
            
            if (metrics.CascadingWrites > 0)
            {
                sb.AppendLine($"  <color=yellow>Cascading Writes: {metrics.CascadingWrites} (writes triggering other writes)</color>");
                sb.AppendLine($"  Cascade Ratio: {(metrics.VariableWrites > 0 ? (float)metrics.CascadingWrites / metrics.VariableWrites * 100 : 0):F1}%");
            }
            
            sb.AppendLine($"  <color=yellow>Unity Equivalent: Direct field access (~0.001 μs)</color>");
            sb.AppendLine();

            // 3. ScriptableObject Lifecycle Overhead
            sb.AppendLine("┌────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("│ <b>3. SCRIPTABLEOBJECT LIFECYCLE OVERHEAD</b>                       │");
            sb.AppendLine("└────────────────────────────────────────────────────────────────┘");
            sb.AppendLine($"  Total SO Assets Loaded: {metrics.ScriptableObjectsLoaded}");
            sb.AppendLine($"  <color=yellow>Unity Equivalent: 0 SOs (all state in MonoBehaviour fields)</color>");
            sb.AppendLine();

            // 4. Instancer Cost vs Local State
            sb.AppendLine("┌────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("│ <b>4. INSTANCER COST vs LOCAL STATE</b>                            │");
            sb.AppendLine("└────────────────────────────────────────────────────────────────┘");
            sb.AppendLine($"  Instancers Created: {metrics.InstancersCreated}");
            
            if (metrics.InstancersCreated > 0)
            {
                sb.AppendLine($"  Avg Creation Time: {(metrics.InstancerCreationTime / metrics.InstancersCreated):F3} ms");
                sb.AppendLine($"  Total Creation Overhead: {metrics.InstancerCreationTime:F2} ms");
            }
            
            sb.AppendLine($"  <color=yellow>Unity Equivalent: Local fields (instantaneous initialization)</color>");
            sb.AppendLine();

            // 5. Reactive Collection Cost
            sb.AppendLine("┌────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("│ <b>5. REACTIVE COLLECTION COST</b>                                  │");
            sb.AppendLine("└────────────────────────────────────────────────────────────────┘");
            sb.AppendLine($"  Collection Adds: {metrics.CollectionAdds:N0}");
            sb.AppendLine($"  Collection Removes: {metrics.CollectionRemoves:N0}");
            
            if (metrics.CollectionAdds > 0 && metrics.CollectionRemoves > 0)
            {
                float avgAddTime = 0;
                float avgRemoveTime = 0;
                
                // Calculate from profiler data if available
                sb.AppendLine($"  Total Modifications: {metrics.CollectionAdds + metrics.CollectionRemoves:N0}");
            }
            
            sb.AppendLine($"  <color=yellow>Unity Equivalent: List<T> Add/Remove (~0.01-0.1 μs)</color>");
            sb.AppendLine();

            // 6. Memory Behavior Under Load
            sb.AppendLine("┌────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("│ <b>7. MEMORY BEHAVIOR UNDER LOAD</b>                                │");
            sb.AppendLine("└────────────────────────────────────────────────────────────────┘");
            sb.AppendLine($"  Atoms Allocations: {FormatBytes(metrics.AllocationBytes)}");
            sb.AppendLine($"  GC Spikes: {metrics.GCSpikes}");
            
            if (metrics.HeapFragmentation > 0)
            {
                sb.AppendLine($"  Est. Heap Fragmentation: {(metrics.HeapFragmentation):F4}");
                
                string fragmentationAssessment = metrics.HeapFragmentation < 0.01f ? "Low" :
                                                metrics.HeapFragmentation < 0.05f ? "Moderate" : "High";
                sb.AppendLine($"  Fragmentation Level: <color={(metrics.HeapFragmentation < 0.01f ? "green" : "yellow")}>{fragmentationAssessment}</color>");
            }
            
            sb.AppendLine($"  <color=yellow>Unity Equivalent: Local fields (minimal allocations)</color>");
            sb.AppendLine();

            // 8. Scheduling & Timing Characteristics
            sb.AppendLine("┌────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("│ <b>8. SCHEDULING & TIMING CHARACTERISTICS</b>                       │");
            sb.AppendLine("└────────────────────────────────────────────────────────────────┘");
            
            if (metrics.AvgEventLatency > 0)
            {
                sb.AppendLine($"  Avg Event Latency: {metrics.AvgEventLatency:F3} ms");
                sb.AppendLine($"  Max Event Latency: {metrics.MaxEventLatency:F3} ms");
                sb.AppendLine($"  Latency Variability: {(metrics.MaxEventLatency / metrics.AvgEventLatency):F2}x");
                sb.AppendLine($"  <color=yellow>Unity Equivalent: Direct calls (~0.001 ms)</color>");
            }
            else
            {
                sb.AppendLine($"  <color=white>No latency data recorded</color>");
            }
            
            sb.AppendLine();

            // 10. Cache Locality & Access Patterns
            sb.AppendLine("┌────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("│ <b>10. CACHE LOCALITY & ACCESS PATTERNS</b>                         │");
            sb.AppendLine("└────────────────────────────────────────────────────────────────┘");
            sb.AppendLine($"  Indirection Lookups: {metrics.IndirectionLookups:N0}");
            
            // Compare to direct access
            if (metrics.IndirectionLookups > 0)
            {
                sb.AppendLine($"  <color=yellow>Each lookup: ScriptableObject reference → Value</color>");
                sb.AppendLine($"  <color=yellow>Unity Equivalent: Direct field access (no indirection)</color>");
                sb.AppendLine($"  <color=yellow>Potential cache misses: {metrics.IndirectionLookups:N0}</color>");
            }
            
            sb.AppendLine();

            // 11-14: EXPANDED SHARED METRICS
            sb.AppendLine("┌────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("│ <b>11. PROJECTILE & SPLASH DAMAGE ANALYSIS (Both)</b>               │");
            sb.AppendLine("└────────────────────────────────────────────────────────────────┘");

            // Get first phase for session-wide metrics
            var firstPhase = result.PhaseComparisons.FirstOrDefault();
            if (firstPhase != null)
            {
                sb.AppendLine($"  Unity Projectiles Spawned: {firstPhase.UnityProjectilesSpawned}");
                sb.AppendLine($"  Atoms Projectiles Spawned: {firstPhase.AtomsProjectilesSpawned}");
                sb.AppendLine($"  Unity Retargets: {firstPhase.UnityProjectileRetargets}");
                sb.AppendLine($"  Atoms Retargets: {firstPhase.AtomsProjectileRetargets}");
                sb.AppendLine($"  Unity Splash Hits: {firstPhase.UnitySplashDamageHits}");
                sb.AppendLine($"  Atoms Splash Hits: {firstPhase.AtomsSplashDamageHits}");
                sb.AppendLine($"  Unity Total Splash Damage: {firstPhase.UnityTotalSplashDamage}");
                sb.AppendLine($"  Atoms Total Splash Damage: {firstPhase.AtomsTotalSplashDamage}");
                
                if (firstPhase.UnityProjectilesSpawned > 0 && firstPhase.AtomsProjectilesSpawned > 0)
                {
                    float unityEfficiency = (float)firstPhase.UnitySplashDamageHits / firstPhase.UnityProjectilesSpawned;
                    float atomsEfficiency = (float)firstPhase.AtomsSplashDamageHits / firstPhase.AtomsProjectilesSpawned;
                    sb.AppendLine($"  Unity Splash Efficiency: {unityEfficiency:F2} hits/projectile");
                    sb.AppendLine($"  Atoms Splash Efficiency: {atomsEfficiency:F2} hits/projectile");
                }
            }
            else
            {
                sb.AppendLine($"  <color=gray>No data available</color>");
            }
            sb.AppendLine();

            // 12. NAVMESH & PATHFINDING ANALYSIS
            sb.AppendLine("┌────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("│ <b>12. NAVMESH & PATHFINDING ANALYSIS (Both)</b>                    │");
            sb.AppendLine("└────────────────────────────────────────────────────────────────┘");

            if (firstPhase != null)
            {
                sb.AppendLine($"  Unity Path Recalculations: {firstPhase.UnityNavMeshRecalculations}");
                sb.AppendLine($"  Atoms Path Recalculations: {firstPhase.AtomsNavMeshRecalculations}");
                sb.AppendLine($"  Unity Stuck Events: {firstPhase.UnityNavMeshStucks}");
                sb.AppendLine($"  Atoms Stuck Events: {firstPhase.AtomsNavMeshStucks}");
                sb.AppendLine($"  Unity Total Path Length: {firstPhase.UnityTotalPathLength:F2}m");
                sb.AppendLine($"  Atoms Total Path Length: {firstPhase.AtomsTotalPathLength:F2}m");
                
                bool navMeshMatch = firstPhase.UnityNavMeshRecalculations == firstPhase.AtomsNavMeshRecalculations &&
                                    firstPhase.UnityNavMeshStucks == firstPhase.AtomsNavMeshStucks;
                sb.AppendLine($"  <color={(navMeshMatch ? "green" : "yellow")}>Behavior: {(navMeshMatch ? "✓ IDENTICAL" : "Minor differences")}</color>");
            }
            else
            {
                sb.AppendLine($"  <color=gray>No data available</color>");
            }
            sb.AppendLine();

            // 13. FSM STATE TRANSITION ANALYSIS
            sb.AppendLine("┌────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("│ <b>13. FSM STATE TRANSITION ANALYSIS (Both)</b>                     │");
            sb.AppendLine("└────────────────────────────────────────────────────────────────┘");

            if (firstPhase != null)
            {
                sb.AppendLine($"  Unity State Transitions: {firstPhase.UnityStateTransitions}");
                sb.AppendLine($"  Atoms State Transitions: {firstPhase.AtomsStateTransitions}");
                
                int transitionDiff = Math.Abs(firstPhase.UnityStateTransitions - firstPhase.AtomsStateTransitions);
                sb.AppendLine($"  Difference: {transitionDiff}");
                
                bool fsmMatch = firstPhase.UnityStateTransitions == firstPhase.AtomsStateTransitions;
                sb.AppendLine($"  <color={(fsmMatch ? "green" : "yellow")}>FSM Logic: {(fsmMatch ? "✓ IDENTICAL" : "Minor differences")}</color>");
            }
            else
            {
                sb.AppendLine($"  <color=gray>No data available</color>");
            }
            sb.AppendLine();

            // 14. ANIMATION SYSTEM ANALYSIS
            sb.AppendLine("┌────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("│ <b>14. ANIMATION SYSTEM ANALYSIS (Both)</b>                         │");
            sb.AppendLine("└────────────────────────────────────────────────────────────────┘");

            if (firstPhase != null)
            {
                sb.AppendLine($"  Unity Animation Triggers: {firstPhase.UnityAnimationTriggers}");
                sb.AppendLine($"  Atoms Animation Triggers: {firstPhase.AtomsAnimationTriggers}");
                
                int animDiff = Math.Abs(firstPhase.UnityAnimationTriggers - firstPhase.AtomsAnimationTriggers);
                sb.AppendLine($"  Difference: {animDiff}");
                
                bool animMatch = firstPhase.UnityAnimationTriggers == firstPhase.AtomsAnimationTriggers;
                sb.AppendLine($"  <color={(animMatch ? "green" : "yellow")}>Animation Logic: {(animMatch ? "✓ IDENTICAL" : "Minor differences")}</color>");
            }
            else
            {
                sb.AppendLine($"  <color=gray>No data available</color>");
            }
            sb.AppendLine();
        }

        // Overall Summary (REMOVED closing remarks as requested)
        sb.AppendLine("╔══════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║                      OVERALL SUMMARY                             ║");
        sb.AppendLine("╚══════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();
        sb.AppendLine($"<b>Unity Wins:</b> {result.UnityWins} metrics");
        sb.AppendLine($"<b>Atoms Wins:</b> {result.AtomsWins} metrics");
        sb.AppendLine();
        sb.AppendLine($"<size=16><color={(result.AtomsWins > result.UnityWins ? "green" : "yellow")}><b>OVERALL WINNER: {result.OverallWinner}</b></color></size>");

        _comparisonText.text = sb.ToString();

        // Force layout rebuild and scroll to top
        if (_comparisonScrollView != null)
        {
            Canvas.ForceUpdateCanvases();
            _comparisonScrollView.verticalNormalizedPosition = 1f;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_comparisonText.rectTransform);
        }
    }

    private string FormatDifference(float value, bool higherIsBetter)
    {
        string sign = value >= 0 ? "+" : "";
        return $"{sign}{value:F2}";
    }

    private string FormatPercent(float value, bool higherIsBetter)
    {
        string sign = value >= 0 ? "+" : "";
        return $"{sign}{value:F1}%";
    }

    private string FormatBytes(long bytes)
    {
        if (bytes >= 1024 * 1024)
            return $"{bytes / (1024f * 1024f):F2} MB";
        if (bytes >= 1024)
            return $"{bytes / 1024f:F2} KB";
        return $"{bytes} B";
    }

    // ADDED: Copy to clipboard functionality
    private void OnCopyToClipboardClicked()
    {
        if (_comparisonText == null || string.IsNullOrEmpty(_comparisonText.text))
        {
            Debug.LogWarning("[PerformanceComparison] No comparison text to copy");
            return;
        }

        // Strip rich text tags for clipboard
        string plainText = StripRichTextTags(_comparisonText.text);
        
        GUIUtility.systemCopyBuffer = plainText;
        Debug.Log("[PerformanceComparison] Copied comparison to clipboard");

        // Show success message
        if (_clipboardSuccessText != null)
        {
            StartCoroutine(ShowClipboardSuccess());
        }
    }

    private IEnumerator ShowClipboardSuccess()
    {
        _clipboardSuccessText.gameObject.SetActive(true);
        _clipboardSuccessText.text = "Text copied successfully!";
        _clipboardSuccessText.color = new Color(0, 1, 0, 1); // Full opacity green

        // Fade out over 2 seconds
        float duration = 2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            _clipboardSuccessText.color = new Color(0, 1, 0, alpha);
            yield return null;
        }

        _clipboardSuccessText.gameObject.SetActive(false);
    }

    private string StripRichTextTags(string richText)
    {
        // Remove all rich text tags
        return System.Text.RegularExpressions.Regex.Replace(richText, "<.*?>", string.Empty);
    }

    private void OnExportComparisonClicked()
    {
        if (_lastComparison == null)
        {
            Debug.LogWarning("[PerformanceComparison] No comparison to export");
            return;
        }

        string directory = Path.Combine(Application.persistentDataPath, "PerformanceData");
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename = $"Comparison_{_lastComparison.UnitySessionId}_vs_{_lastComparison.AtomsSessionId}_{timestamp}.txt";
        string filepath = Path.Combine(directory, filename);

        try
        {
            // Export as plain text (without rich text tags)
            string plainText = StripRichTextTags(_comparisonText.text);
            File.WriteAllText(filepath, plainText);

            // Also export as CSV for graphing
            ExportComparisonCSV(directory, timestamp);

            Debug.Log($"[PerformanceComparison] Exported comparison to: {filepath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PerformanceComparison] Export failed: {e.Message}");
        }
    }

    private void ExportComparisonCSV(string directory, string timestamp)
    {
        string filename = $"Comparison_{_lastComparison.UnitySessionId}_vs_{_lastComparison.AtomsSessionId}_{timestamp}.csv";
        string filepath = Path.Combine(directory, filename);

        var sb = new System.Text.StringBuilder();
        
        // Comprehensive header
        sb.AppendLine("Phase,Metric,Unity,Atoms,Difference,PercentChange,Winner");

        foreach (var phase in _lastComparison.PhaseComparisons)
        {
            // Core Performance Metrics
            sb.AppendLine($"{phase.PhaseName},AvgFPS,{phase.UnityAvgFPS:F2},{phase.AtomsAvgFPS:F2},{phase.FPSDifference:F2},{phase.FPSPercentChange:F2},{(phase.FPSDifference > 0 ? "Atoms" : "Unity")}");
            sb.AppendLine($"{phase.PhaseName},MinFPS,{phase.UnityMinFPS:F2},{phase.AtomsMinFPS:F2},{phase.AtomsMinFPS - phase.UnityMinFPS:F2},N/A,{(phase.AtomsMinFPS > phase.UnityMinFPS ? "Atoms" : "Unity")}");
            sb.AppendLine($"{phase.PhaseName},AvgFrameTime,{phase.UnityAvgFrameTime:F3},{phase.AtomsAvgFrameTime:F3},{phase.FrameTimeDifference:F3},{phase.FrameTimePercentChange:F2},{(phase.FrameTimeDifference > 0 ? "Atoms" : "Unity")}");
            
            // Main Thread Time
            sb.AppendLine($"{phase.PhaseName},MainThreadTime,{phase.UnityMainThreadTime:F3},{phase.AtomsMainThreadTime:F3},{phase.MainThreadTimeDiff:F3},N/A,{(phase.MainThreadTimeDiff > 0 ? "Atoms" : "Unity")}");
            
            // Memory
            sb.AppendLine($"{phase.PhaseName},PeakMemoryMB,{phase.UnityPeakMemory:F2},{phase.AtomsPeakMemory:F2},{phase.MemoryDifference:F2},{phase.MemoryPercentChange:F2},{(phase.MemoryDifference > 0 ? "Atoms" : "Unity")}");
            sb.AppendLine($"{phase.PhaseName},TotalGCAlloc,{phase.UnityTotalGCAlloc},{phase.AtomsTotalGCAlloc},{phase.GCAllocDiff},N/A,{(phase.GCAllocDiff > 0 ? "Atoms" : "Unity")}");
            
            // GC Generation Counts
            sb.AppendLine($"{phase.PhaseName},GC0,{phase.UnityGC0},{phase.AtomsGC0},{phase.UnityGC0 - phase.AtomsGC0},N/A,{(phase.UnityGC0 > phase.AtomsGC0 ? "Atoms" : "Unity")}");
            sb.AppendLine($"{phase.PhaseName},GC1,{phase.UnityGC1},{phase.AtomsGC1},N/A,N/A,Equal");
            sb.AppendLine($"{phase.PhaseName},GC2,{phase.UnityGC2},{phase.AtomsGC2},N/A,N/A,Equal");
            
            // Attacks and Damage (Simulation phase only)
            if (phase.PhaseName == "Simulate")
            {
                sb.AppendLine($"{phase.PhaseName},TotalAttacks,{phase.UnityAttacks},{phase.AtomsAttacks},N/A,N/A,Equal");
                sb.AppendLine($"{phase.PhaseName},TotalDamage,{phase.UnityDamage},{phase.AtomsDamage},N/A,N/A,Equal");
            }
            
            // Shared Metrics
            sb.AppendLine($"{phase.PhaseName},ProjectilesSpawned,{phase.UnityProjectilesSpawned},{phase.AtomsProjectilesSpawned},N/A,N/A,Equal");
            sb.AppendLine($"{phase.PhaseName},ProjectileRetargets,{phase.UnityProjectileRetargets},{phase.AtomsProjectileRetargets},N/A,N/A,Equal");
            sb.AppendLine($"{phase.PhaseName},SplashDamageHits,{phase.UnitySplashDamageHits},{phase.AtomsSplashDamageHits},N/A,N/A,Equal");
            sb.AppendLine($"{phase.PhaseName},TotalSplashDamage,{phase.UnityTotalSplashDamage},{phase.AtomsTotalSplashDamage},N/A,N/A,Equal");
            sb.AppendLine($"{phase.PhaseName},NavMeshRecalculations,{phase.UnityNavMeshRecalculations},{phase.AtomsNavMeshRecalculations},N/A,N/A,Equal");
            sb.AppendLine($"{phase.PhaseName},NavMeshStucks,{phase.UnityNavMeshStucks},{phase.AtomsNavMeshStucks},N/A,N/A,Equal");
            sb.AppendLine($"{phase.PhaseName},TotalPathLength,{phase.UnityTotalPathLength},{phase.AtomsTotalPathLength},N/A,N/A,Equal");
            sb.AppendLine($"{phase.PhaseName},StateTransitions,{phase.UnityStateTransitions},{phase.AtomsStateTransitions},N/A,N/A,Equal");
            sb.AppendLine($"{phase.PhaseName},AnimationTriggers,{phase.UnityAnimationTriggers},{phase.AtomsAnimationTriggers},N/A,N/A,Equal");
        }

        // Write to file
        try
        {
            File.WriteAllText(filepath, sb.ToString());
            Debug.Log($"[PerformanceComparison] Exported comparison CSV to: {filepath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PerformanceComparison] CSV export failed: {e.Message}");
        }
    }

    private void DisplaySavePath()
    {
        if (_savePathText == null) return;
        
        string savePath = Path.Combine(Application.persistentDataPath, "PerformanceData");
        _savePathText.text = $"<b>Save Path:</b> {savePath}";
        
        // Make text selectable (requires TextMeshPro settings)
        // In the Unity Inspector, enable "Enable Text Selection" on the TextMeshProUGUI component
        Debug.Log($"[PerformanceComparison] Data files saved to: {savePath}");
    }

    // ========== DATA STRUCTURES ==========
    
    [System.Serializable]
    private class SessionData
    {
        public string Mode;
        public string SessionId;
        public double TotalDuration;
        public List<PhaseData> Phases;
        public AtomsMetricsData AtomsMetrics;
    }

    [System.Serializable]
    private class PhaseData
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
        
        // Enhanced
        public float AvgMainThreadTime;
        public float MaxMainThreadTime;
        public long TotalGCAlloc;
        public float AvgDrawCalls;
        public float AvgBatches;
        
        // ADDED: Shared metrics
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

    private class ComparisonResult
    {
        public string UnitySessionId;
        public string AtomsSessionId;
        public List<PhaseComparison> PhaseComparisons;
        public int UnityWins;
        public int AtomsWins;
        public string OverallWinner;

        public void CalculateWinner()
        {
            UnityWins = 0;
            AtomsWins = 0;

            foreach (var phase in PhaseComparisons)
            {
                // FPS: Higher is better
                if (phase.FPSDifference > 0) AtomsWins++; else UnityWins++;
                
                // Frame Time: Lower is better (Unity's is better if positive difference)
                if (phase.FrameTimeDifference > 0) AtomsWins++; else UnityWins++;
                
                // Main Thread Time: Lower is better
                if (phase.MainThreadTimeDiff > 0) AtomsWins++; else UnityWins++;
                
                // Memory: Lower is better (Unity's is better if positive difference)
                if (phase.MemoryDifference > 0) AtomsWins++; else UnityWins++;
                
                // GC Gen0: Fewer is better (Unity's is better if positive difference)
                if (phase.GC0Difference > 0) AtomsWins++; else UnityWins++;
                
                // GC Allocations: Less is better (Unity's is better if positive difference)
                if (phase.GCAllocDiff > 0) AtomsWins++; else UnityWins++;
            }

            OverallWinner = AtomsWins > UnityWins ? "ATOMS" : AtomsWins < UnityWins ? "UNITY" : "TIE";
        }
    }

    private class PhaseComparison
    {
        public string PhaseName;

        // Basic
        public float UnityAvgFPS;
        public float AtomsAvgFPS;
        public float FPSDifference;
        public float FPSPercentChange;

        public float UnityMinFPS;
        public float AtomsMinFPS;

        public float UnityAvgFrameTime;
        public float AtomsAvgFrameTime;
        public float FrameTimeDifference;
        public float FrameTimePercentChange;

        public float UnityPeakMemory;
        public float AtomsPeakMemory;
        public float MemoryDifference;
        public float MemoryPercentChange;

        public int UnityGC0;
        public int AtomsGC0;
        public int GC0Difference;

        public int UnityGC1;
        public int AtomsGC1;

        public int UnityGC2;
        public int AtomsGC2;

        public int UnityAttacks;
        public int AtomsAttacks;

        public int UnityDamage;
        public int AtomsDamage;

        // Enhanced
        public float UnityMainThreadTime;
        public float AtomsMainThreadTime;
        public float MainThreadTimeDiff;

        public long UnityTotalGCAlloc;
        public long AtomsTotalGCAlloc;
        public long GCAllocDiff;
        
        // ADDED: Shared metrics comparison
        public int UnityProjectilesSpawned;
        public int AtomsProjectilesSpawned;
        public int UnityProjectileRetargets;
        public int AtomsProjectileRetargets;
        public int UnitySplashDamageHits;
        public int AtomsSplashDamageHits;
        public int UnityTotalSplashDamage;
        public int AtomsTotalSplashDamage;
        
        public int UnityNavMeshRecalculations;
        public int AtomsNavMeshRecalculations;
        public int UnityNavMeshStucks;
        public int AtomsNavMeshStucks;
        public float UnityTotalPathLength;
        public float AtomsTotalPathLength;
        
        public int UnityStateTransitions;
        public int AtomsStateTransitions;
        
        public int UnityAnimationTriggers;
        public int AtomsAnimationTriggers;
    }
} // End of PerformanceComparison class