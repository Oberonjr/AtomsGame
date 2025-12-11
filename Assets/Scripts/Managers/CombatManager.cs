using UnityEngine;

/// <summary>
/// Unity implementation of combat management
/// </summary>
public class CombatManager : CombatManagerBase
{
    private static CombatManager _instance;
    public static CombatManager Instance => _instance;
    
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            Debug.Log("[CombatManager] Instance created");
        }
        else
        {
            Debug.LogWarning("[CombatManager] Duplicate instance destroyed");
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
    
    // That's it! Everything else is inherited from CombatManagerBase
}