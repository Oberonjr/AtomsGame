using UnityAtoms.BaseAtoms;
using UnityEngine;

/// <summary>
/// Atoms implementation of combat management with reactive events
/// </summary>
public class CombatManager_Atoms : CombatManagerBase
{
    private static CombatManager_Atoms _instance;
    public static CombatManager_Atoms Instance => _instance;
    
    [Header("Atoms Events (Optional)")]
    [SerializeField] private VoidEvent _onCombatStart;
    [SerializeField] private VoidEvent _onCombatEnd;
    [SerializeField] private VoidEvent _onTargetAssigned;
    
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            Debug.Log("[CombatManager_Atoms] Instance created");
        }
        else
        {
            Debug.LogWarning("[CombatManager_Atoms] Duplicate instance destroyed");
            Destroy(gameObject);
        }
    }
    
    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
    
    // ========== OVERRIDE HOOKS TO ADD ATOMS EVENTS ==========

    protected override void OnInitialize()
    {
        // ADDED: Track event dispatch
        if (_onCombatStart != null)
        {
            AtomsPerformanceTracker.TrackEventDispatch();
            _onCombatStart.Raise();
        }
    }
    
    protected override void OnCleanup()
    {
        // ADDED: Track event dispatch
        if (_onCombatEnd != null)
        {
            AtomsPerformanceTracker.TrackEventDispatch();
            _onCombatEnd.Raise();
        }
    }
    
    protected override void OnTargetAssigned(ITroop troop, ITroop target)
    {
        // ADDED: Track event dispatch with latency
        var latencyToken = AtomsPerformanceTracker.StartEventLatencyTracking();
        
        if (_onTargetAssigned != null)
        {
            AtomsPerformanceTracker.TrackEventDispatch();
            _onTargetAssigned.Raise();
        }
        
        AtomsPerformanceTracker.EndEventLatencyTracking(latencyToken);
    }
}