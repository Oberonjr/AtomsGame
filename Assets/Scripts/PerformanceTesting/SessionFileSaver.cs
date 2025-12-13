using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Udar.SceneManager;

/// <summary>
/// Ensures a single session file is saved when leaving the game scene or quitting while in the game scene.
/// - Buffer session JSON via SetPendingSessionJson
/// - Start a session with BeginSession
/// - File is written once on scene unload (from gameSceneName) or OnApplicationQuit if active scene is gameSceneName
/// </summary>
public class SessionFileSaver : MonoBehaviour
{
    private static SessionFileSaver _instance;
    public static SessionFileSaver Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("SessionFileSaver");
                _instance = go.AddComponent<SessionFileSaver>();
            }
            return _instance;
        }
    }

    [Tooltip("Name of the scene that represents the in-game scene. When leaving this scene a session file will be written.")]
    [SerializeField] private SceneField gameScene;

    private string _pendingJson;
    private SimulationMode _pendingMode = SimulationMode.Unity;
    private string _sessionId;
    private bool _sessionActive;
    private bool _savedForSession;
    private bool _isWaitingToSave = false;
    private bool _readyToWrite = false;
    private string _sessionTimestamp;
    private string _baseFilename;
    // Buffer for multiple files to write at session end
    private Dictionary<string, string> _pendingFiles = new Dictionary<string, string>();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Begin a new session. Resets internal saved flag so one file will be generated when leaving the game scene.
    /// </summary>
    public void BeginSession(string sessionId, SimulationMode mode)
    {
        _sessionId = sessionId ?? Guid.NewGuid().ToString();
        _pendingMode = mode;
        _sessionActive = true;
        _savedForSession = false;
        _pendingJson = null;
        _pendingFiles.Clear();
        _sessionTimestamp = DateTime.Now.ToString("dd-MM-yy_HH-mm-ss");
        _baseFilename = $"({_pendingMode})DataFile_{_sessionTimestamp}";
        Debug.Log($"[SessionFileSaver] Session begun: {_sessionId} Mode: {_pendingMode}");
        _readyToWrite = false;
    }

    /// <summary>
    /// Provide the JSON (or any string) to persist when the session ends.
    /// The writer will only write once per session.
    /// </summary>
    public void SetPendingSessionJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return;

        _pendingJson = json;
        // Buffer JSON under standard filename
        BufferFile("Data.json", json);
        Debug.Log("[SessionFileSaver] Pending session JSON set (buffered).");
    }

    /// <summary>
    /// Buffer an arbitrary file to be written when the session ends.
    /// filenameSuffix should include extension, e.g. "Summary.txt" or "Prep.csv".
    /// </summary>
    public void BufferFile(string filenameSuffix, string content)
    {
        if (string.IsNullOrEmpty(filenameSuffix) || content == null) return;
        if (string.IsNullOrEmpty(_baseFilename))
        {
            _sessionTimestamp = DateTime.Now.ToString("dd-MM-yy_HH-mm-ss");
            _baseFilename = $"({_pendingMode})DataFile_{_sessionTimestamp}";
        }

        string filename = $"{_baseFilename}_{filenameSuffix}";
        _pendingFiles[filename] = content;
        Debug.Log($"[SessionFileSaver] Buffered file: {filename}, Size: {content.Length} chars");
    }

    /// <summary>
    /// Force saving immediately regardless of scene state (still respects single-save-per-session).
    /// </summary>
    public void ForceSave()
    {
        // Only allow forced save in the context of scene unload semantics so we don't create files at start.
        TrySave("scene_unload: forced_save");
    }

    private void OnSceneUnloaded(Scene scene)
    {
        // Only save when the GameScene is unloaded
        if (_sessionActive && !_savedForSession && scene.name == gameScene.Name)
        {
            // Defer actual save slightly to allow other listeners (like PerformanceProfiler.OnSceneUnloaded)
            // to run and buffer their files. Waiting a couple frames avoids race conditions.
            if (!_isWaitingToSave && !_savedForSession)
                StartCoroutine(DeferredSave($"scene_unload: {scene.name}"));
        }
    }

    private void OnActiveSceneChanged(Scene previous, Scene next)
    {
        // If active scene changed away from the game scene, request save
        if (_sessionActive && !_savedForSession && previous.name == gameScene.Name && next.name != gameScene.Name)
        {
            Debug.Log($"[SessionFileSaver] Active scene changed: {previous.name} -> {next.name}");
            if (!_isWaitingToSave && !_savedForSession)
                StartCoroutine(DeferredSave($"active_scene_change: {previous.name} -> {next.name}"));
        }
    }

    private System.Collections.IEnumerator DeferredSave(string reason)
    {
        Debug.Log($"[SessionFileSaver] DeferredSave started for reason: {reason}");
        _isWaitingToSave = true;
        // Give other listeners time to buffer their files (Profiler.ExportData). Use realtime wait.
        yield return new WaitForSecondsRealtime(0.5f);

        Debug.Log("[SessionFileSaver] DeferredSave invoking TrySave");
        TrySave(reason);
        _isWaitingToSave = false;
    }

    private void OnApplicationQuit()
    {
        // If app quits while in the game scene, save pending session
        var active = SceneManager.GetActiveScene();
        if (_sessionActive && !_savedForSession && active.name == gameScene.Name)
        {
            TrySave("application_quit");
        }
    }

    private void TrySave(string reason)
    {
        if (_savedForSession)
        {
            Debug.Log("[SessionFileSaver] Save already performed for this session; skipping.");
            return;
        }

        // Only allow saves triggered by scene unload or application quit.
        // Prevent accidental saves at simulation start or other times.
        if (string.IsNullOrEmpty(reason) ||
            !(reason.StartsWith("scene_unload", StringComparison.OrdinalIgnoreCase) ||
            reason.Equals("application_quit", StringComparison.OrdinalIgnoreCase)))
        {
            Debug.Log($"[SessionFileSaver] TrySave called with non-unload reason '{reason}' - skipping save.");
            return;
        }

        // Do not perform write until producer marks buffered data as ready.
        if (!_readyToWrite)
        {
            Debug.LogWarning("[SessionFileSaver] TrySave invoked before producer marked ready; deferring save.");
            if (!_isWaitingToSave)
                StartCoroutine(WaitAndSave(reason));
            return;
        }

        try
        {
            // Require that JSON is buffered (or _pendingJson is set) before performing actual save.
            bool hasBufferedJson = false;
            foreach (var key in _pendingFiles.Keys)
            {
                if (key.EndsWith("_Data.json", StringComparison.OrdinalIgnoreCase) || key.EndsWith("_Data.json", StringComparison.Ordinal))
                {
                    hasBufferedJson = true;
                    break;
                }
            }

            if (string.IsNullOrEmpty(_pendingJson) && !hasBufferedJson)
            {
                Debug.LogWarning("[SessionFileSaver] No JSON buffered yet; deferring save until JSON is available.");
                if (!_isWaitingToSave)
                    StartCoroutine(WaitAndSave(reason));
                return;
            }

            // Ensure at least one pending file has meaningful content (avoid empty writes at simulation start)
            bool hasMeaningfulContent = !string.IsNullOrEmpty(_pendingJson) && _pendingJson.Length >16;
            // Prefer explicit JSON check: require non-empty Phases array in JSON to consider it a complete export
            bool jsonHasPhases = false;
            if (!string.IsNullOrEmpty(_pendingJson))
            {
                if (_pendingJson.Contains("\"Phases\":[]") || _pendingJson.Contains("\"Phases\": []"))
                {
                    jsonHasPhases = false;
                }
                else if (_pendingJson.Contains("\"Phases\":"))
                {
                    // There's a Phases property and it's not empty
                    jsonHasPhases = true;
                }
            }

            if (jsonHasPhases)
            {
                hasMeaningfulContent = true;
            }

            if (!hasMeaningfulContent)
            {
                Debug.LogWarning("[SessionFileSaver] No meaningful buffered content yet; deferring save.");
                if (!_isWaitingToSave)
                    StartCoroutine(WaitAndSave(reason));
                return;
            }

            string directory = Path.Combine(Application.persistentDataPath, "PerformanceData");
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            foreach (var file in _pendingFiles)
            {
                string filename = file.Key;
                string content = file.Value;

                string filepath = Path.Combine(directory, filename);
                File.WriteAllText(filepath, content);
                Debug.Log($"[SessionFileSaver] Saved session file ({reason}) to: {filepath}");
            }

            // Also save the JSON data to the standard Data.json file if it hasn't been buffered already
            bool hasBufferedJsonFile = false;
            foreach (var key in _pendingFiles.Keys)
            {
                if (key.EndsWith("_Data.json", StringComparison.OrdinalIgnoreCase))
                {
                    hasBufferedJsonFile = true;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(_pendingJson) && !hasBufferedJsonFile)
            {
                string timestamp = DateTime.Now.ToString("dd-MM-yy_HH-mm-ss");
                string filename = $"({_pendingMode})DataFile_{timestamp}_Data.json";
                string filepath = Path.Combine(directory, filename);

                File.WriteAllText(filepath, _pendingJson);
                Debug.Log($"[SessionFileSaver] Saved session JSON data ({reason}) to: {filepath}");
            }

            // Mark as saved so no more files are generated for this session
            _savedForSession = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SessionFileSaver] Failed to save session file: {e.Message}");
        }
    }

    private System.Collections.IEnumerator WaitAndSave(string reason)
    {
        _isWaitingToSave = true;
        float timeout =2.0f; // seconds
        float elapsed =0f;
        float interval =0.1f;

        Debug.Log($"[SessionFileSaver] WaitAndSave: waiting up to {timeout}s for pending files to appear...");

        while (elapsed < timeout)
        {
            if (!string.IsNullOrEmpty(_pendingJson) || _pendingFiles.Count >0)
            {
                Debug.Log("[SessionFileSaver] Pending data detected during wait; proceeding to save.");
                TrySave(reason);
                _isWaitingToSave = false;
                yield break;
            }

            yield return new WaitForSecondsRealtime(interval);
            elapsed += interval;
        }

        Debug.LogWarning("[SessionFileSaver] WaitAndSave timed out; no pending data was provided. Skipping save.");
        _savedForSession = true; // mark as done to avoid repeated waits
        _isWaitingToSave = false;
    }

    /// <summary>
    /// Call this when buffered files are ready to be written (e.g. PerformanceProfiler finished ExportData).
    /// </summary>
    public void MarkReadyToSave()
    {
        _readyToWrite = true;
        Debug.Log("[SessionFileSaver] Marked ready to save (buffered files present).");
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnloaded;
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }
}