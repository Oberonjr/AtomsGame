using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Udar.SceneManager;

/// <summary>
/// Manages the Performance Comparison scene - loads, compares, and displays performance data
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

    [Header("Scene Settings")]
    [SerializeField] private SceneField _mainMenuScene;

    private SessionData _unityData;
    private SessionData _atomsData;
    private SimulationMode _currentLoadingMode;
    private PhaseFilter _currentPhaseFilter = PhaseFilter.All;
    private ComparisonResult _lastComparison;

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
        UpdateUI();
        
        // Hide clipboard success text initially
        if (_clipboardSuccessText != null)
            _clipboardSuccessText.gameObject.SetActive(false);
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

        var jsonFiles = Directory.GetFiles(directory, $"{mode}_*_Data.json")
                                 .OrderByDescending(f => File.GetCreationTime(f))
                                 .ToList();

        if (jsonFiles.Count == 0)
        {
            Debug.LogWarning($"[PerformanceComparison] No {mode} data files found");
            CreateNoFilesMessage(mode);
            _fileSelectionPanel.SetActive(true);
            return;
        }

        // Create button for each file
        foreach (string filePath in jsonFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string displayName = fileName.Replace($"{mode}_", "").Replace("_Data", "");

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

            // Also delete associated files (Summary, CSVs)
            string directory = Path.GetDirectoryName(filePath);
            string filePrefix = Path.GetFileNameWithoutExtension(filePath).Replace("_Data", "");
            
            var relatedFiles = Directory.GetFiles(directory, $"{filePrefix}*");
            foreach (var file in relatedFiles)
            {
                if (File.Exists(file))
                    File.Delete(file);
            }

            Debug.Log($"[PerformanceComparison] Deleted {displayName} and associated files");

            // Refresh the file list
            ShowFileSelectionPanel(_currentLoadingMode);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PerformanceComparison] Failed to delete file: {e.Message}");
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

        // Filter phases based on selection
        var phasesToCompare = GetPhasesToCompare(unity, atoms, filter);

        foreach (var phasePair in phasesToCompare)
        {
            var unityPhase = phasePair.Item1;
            var atomsPhase = phasePair.Item2;

            var phaseComp = new PhaseComparison
            {
                PhaseName = unityPhase.Phase,

                // FPS Metrics
                UnityAvgFPS = unityPhase.AvgFPS,
                AtomsAvgFPS = atomsPhase.AvgFPS,
                FPSDifference = atomsPhase.AvgFPS - unityPhase.AvgFPS,
                FPSPercentChange = CalculatePercentChange(unityPhase.AvgFPS, atomsPhase.AvgFPS),

                UnityMinFPS = unityPhase.MinFPS,
                AtomsMinFPS = atomsPhase.MinFPS,

                // Frame Time Metrics
                UnityAvgFrameTime = unityPhase.AvgFrameTime,
                AtomsAvgFrameTime = atomsPhase.AvgFrameTime,
                FrameTimeDifference = unityPhase.AvgFrameTime - atomsPhase.AvgFrameTime,
                FrameTimePercentChange = CalculatePercentChange(unityPhase.AvgFrameTime, atomsPhase.AvgFrameTime),

                // Memory Metrics
                UnityPeakMemory = unityPhase.PeakMemoryMB,
                AtomsPeakMemory = atomsPhase.PeakMemoryMB,
                MemoryDifference = unityPhase.PeakMemoryMB - atomsPhase.PeakMemoryMB,
                MemoryPercentChange = CalculatePercentChange(unityPhase.PeakMemoryMB, atomsPhase.PeakMemoryMB),

                // GC Metrics
                UnityGC0 = unityPhase.GC0,
                AtomsGC0 = atomsPhase.GC0,
                GC0Difference = unityPhase.GC0 - atomsPhase.GC0,

                UnityGC1 = unityPhase.GC1,
                AtomsGC1 = atomsPhase.GC1,

                UnityGC2 = unityPhase.GC2,
                AtomsGC2 = atomsPhase.GC2,

                // Combat Metrics (for Simulate phase)
                UnityAttacks = unityPhase.TotalAttacks,
                AtomsAttacks = atomsPhase.TotalAttacks,
                UnityDamage = unityPhase.TotalDamage,
                AtomsDamage = atomsPhase.TotalDamage
            };

            result.PhaseComparisons.Add(phaseComp);
        }

        // Calculate overall winner
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
            // FIXED: Use proper rich text format
            sb.AppendLine($"  <color={(phase.FPSDifference > 0 ? "green" : "orange")}>Winner: {(phase.FPSDifference > 0 ? "Atoms" : "Unity")}</color>");
            sb.AppendLine();

            // Frame Time Comparison
            sb.AppendLine("<b>─── Frame Time (ms) ───</b>");
            sb.AppendLine($"  Unity Avg:    {phase.UnityAvgFrameTime:F3}");
            sb.AppendLine($"  Atoms Avg:    {phase.AtomsAvgFrameTime:F3}");
            sb.AppendLine($"  Improvement:  {FormatDifference(phase.FrameTimeDifference, false)} ({FormatPercent(phase.FrameTimePercentChange, false)})");
            sb.AppendLine($"  <color={(phase.FrameTimeDifference > 0 ? "green" : "orange")}>Winner: {(phase.FrameTimeDifference > 0 ? "Atoms" : "Unity")}</color>");
            sb.AppendLine();

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

        // Overall Summary
        sb.AppendLine("╔══════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║                      OVERALL SUMMARY                             ║");
        sb.AppendLine("╚══════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();
        sb.AppendLine($"<b>Unity Wins:</b> {result.UnityWins}");
        sb.AppendLine($"<b>Atoms Wins:</b> {result.AtomsWins}");
        sb.AppendLine();
        sb.AppendLine($"<size=16><color={(result.AtomsWins > result.UnityWins ? "green" : "yellow")}><b>OVERALL WINNER: {result.OverallWinner}</b></color></size>");

        _comparisonText.text = sb.ToString();

        // FIXED: Force layout rebuild and scroll to top
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
        sb.AppendLine("Phase,Metric,Unity,Atoms,Difference,PercentChange,Winner");

        foreach (var phase in _lastComparison.PhaseComparisons)
        {
            // FPS
            sb.AppendLine($"{phase.PhaseName},AvgFPS,{phase.UnityAvgFPS:F2},{phase.AtomsAvgFPS:F2},{phase.FPSDifference:F2},{phase.FPSPercentChange:F2},{(phase.FPSDifference > 0 ? "Atoms" : "Unity")}");

            // Frame Time
            sb.AppendLine($"{phase.PhaseName},AvgFrameTime,{phase.UnityAvgFrameTime:F3},{phase.AtomsAvgFrameTime:F3},{phase.FrameTimeDifference:F3},{phase.FrameTimePercentChange:F2},{(phase.FrameTimeDifference > 0 ? "Atoms" : "Unity")}");

            // Memory
            sb.AppendLine($"{phase.PhaseName},PeakMemory,{phase.UnityPeakMemory:F2},{phase.AtomsPeakMemory:F2},{phase.MemoryDifference:F2},{phase.MemoryPercentChange:F2},{(phase.MemoryDifference > 0 ? "Atoms" : "Unity")}");

            // GC
            sb.AppendLine($"{phase.PhaseName},GC0,{phase.UnityGC0},{phase.AtomsGC0},{phase.GC0Difference},N/A,{(phase.GC0Difference > 0 ? "Atoms" : "Unity")}");
        }

        File.WriteAllText(filepath, sb.ToString());
    }

    // Data structures (unchanged)
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
                if (phase.FPSDifference > 0) AtomsWins++; else UnityWins++;
                if (phase.FrameTimeDifference > 0) AtomsWins++; else UnityWins++;
                if (phase.MemoryDifference > 0) AtomsWins++; else UnityWins++;
                if (phase.GC0Difference > 0) AtomsWins++; else UnityWins++;
            }

            OverallWinner = AtomsWins > UnityWins ? "ATOMS" : AtomsWins < UnityWins ? "UNITY" : "TIE";
        }
    }

    private class PhaseComparison
    {
        public string PhaseName;

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
    }
}