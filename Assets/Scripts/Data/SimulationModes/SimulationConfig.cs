using UnityEngine;

public enum SimulationMode
{
    Unity,
    Atoms
}

/// <summary>
/// Persistent configuration manager for simulation mode selection.
/// Set in MainMenu, persists across scenes.
/// </summary>
public class SimulationConfig : MonoBehaviour
{
    private static SimulationConfig _instance;
    public static SimulationConfig Instance => _instance;

    [Header("Simulation Settings")]
    [SerializeField] private SimulationMode _mode = SimulationMode.Unity;

    [Header("Performance Tracking")]
    [SerializeField] private bool _enableProfiling = true;
    [SerializeField] private int _profileFrameCount = 300;

    [Header("Debug")]
    [SerializeField] private bool _verboseLogging = false;

    // Public properties
    public SimulationMode Mode
    {
        get => _mode;
        set
        {
            _mode = value;
            Debug.Log($"[SimulationConfig] Mode set to: {_mode}");
        }
    }

    public bool EnableProfiling => _enableProfiling;
    public int ProfileFrameCount => _profileFrameCount;
    public bool VerboseLogging => _verboseLogging;

    void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log($"[SimulationConfig] Instance created. Mode: {_mode}");
        }
        else
        {
            Debug.LogWarning($"[SimulationConfig] Duplicate instance destroyed");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Set mode from main menu or other UI
    /// </summary>
    public void SetMode(SimulationMode mode)
    {
        Mode = mode;
    }

    /// <summary>
    /// Set mode from main menu button (Unity mode)
    /// </summary>
    public void SetUnityMode()
    {
        Mode = SimulationMode.Unity;
    }

    /// <summary>
    /// Set mode from main menu button (Atoms mode)
    /// </summary>
    public void SetAtomsMode()
    {
        Mode = SimulationMode.Atoms;
    }
}