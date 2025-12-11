using UnityEngine;

/// <summary>
/// Enables the correct CombatManager based on SimulationConfig mode
/// Attach this to a GameObject in GameScene that has both CombatManager and CombatManager_Atoms
/// </summary>
public class CombatManagerSelector : MonoBehaviour
{
    [Header("Combat Managers")]
    [SerializeField] private CombatManager _unityCombatManager;
    [SerializeField] private CombatManager_Atoms _atomsCombatManager;

    void Awake()
    {
        // Determine which mode to use
        SimulationMode mode = SimulationMode.Unity; // Default
        if (SimulationConfig.Instance != null)
        {
            mode = SimulationConfig.Instance.Mode;
        }

        Debug.Log($"[CombatManagerSelector] Mode: {mode}");

        // Enable/disable based on mode
        if (_unityCombatManager != null)
        {
            _unityCombatManager.enabled = (mode == SimulationMode.Unity);
            Debug.Log($"[CombatManagerSelector] Unity CombatManager: {(mode == SimulationMode.Unity ? "ENABLED" : "DISABLED")}");
        }

        if (_atomsCombatManager != null)
        {
            _atomsCombatManager.enabled = (mode == SimulationMode.Atoms);
            Debug.Log($"[CombatManagerSelector] Atoms CombatManager: {(mode == SimulationMode.Atoms ? "ENABLED" : "DISABLED")}");
        }
    }
}