using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Coordinates game state and delegates to specialized managers
/// </summary>
public class GameStateManager : MonoBehaviour
{
    private static GameStateManager _instance;
    public static GameStateManager Instance => _instance;

    [Header("Unit Selection")]
    public List<UnitSelectionData> AvailableUnits = new List<UnitSelectionData>();

    [Header("Components")]
    private UnitSpawner _unitSpawner;
    private InputHandler _inputHandler;
    private SaveLoadManager _saveLoadManager;

    private GameState _currentState = GameState.Prep;
    private UnitSelectionData _selectedUnit;
    private List<ITroop> _activeTroops = new List<ITroop>();
    private bool _isPaused = false;

    public GameState CurrentState => _currentState;
    public UnitSelectionData SelectedUnit => _selectedUnit;
    public bool IsPaused => _isPaused;
    public IReadOnlyList<ITroop> ActiveTroops => _activeTroops;

    public event Action<GameState> OnStateChanged;
    public event Action<UnitSelectionData> OnUnitSelected;
    public event Action<Team> OnTeamWon;
    public event Action OnLayoutCleared;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Get or add components
        if (_unitSpawner == null)
            _unitSpawner = GetComponent<UnitSpawner>() ?? gameObject.AddComponent<UnitSpawner>();
        if (_inputHandler == null)
            _inputHandler = GetComponent<InputHandler>() ?? gameObject.AddComponent<InputHandler>();
        if (_saveLoadManager == null)
            _saveLoadManager = GetComponent<SaveLoadManager>() ?? gameObject.AddComponent<SaveLoadManager>();
        
        Debug.Log("[GameStateManager] Components initialized");
    }

    void Start()
    {
        // Subscribe to component events
        _unitSpawner.OnTroopSpawned += OnTroopSpawned;
        _unitSpawner.OnTroopRemoved += OnTroopRemoved;
        _inputHandler.OnPlaceUnitRequested += OnPlaceUnitRequested;
        _inputHandler.OnRemoveUnitRequested += OnRemoveUnitRequested;
        
        _inputHandler.Enable();
    }

    void OnDestroy()
    {
        if (_unitSpawner != null)
        {
            _unitSpawner.OnTroopSpawned -= OnTroopSpawned;
            _unitSpawner.OnTroopRemoved -= OnTroopRemoved;
        }
        if (_inputHandler != null)
        {
            _inputHandler.OnPlaceUnitRequested -= OnPlaceUnitRequested;
            _inputHandler.OnRemoveUnitRequested -= OnRemoveUnitRequested;
        }
    }

    void OnEnable()
    {
        TeamManager.TeamDefeated += OnTeamDefeated;
        Debug.Log("[GameStateManager] Subscribed to TeamDefeated event");
    }

    void OnDisable()
    {
        TeamManager.TeamDefeated -= OnTeamDefeated;
        Debug.Log("[GameStateManager] Unsubscribed from TeamDefeated event");
    }

    // --- PUBLIC API ---

    public void SelectUnit(UnitSelectionData unit)
    {
        _selectedUnit = unit;
        OnUnitSelected?.Invoke(unit);
    }



    public void StartSimulation()
    {
        if (_currentState != GameState.Prep) return;

        _saveLoadManager.SavePrepState(_activeTroops);

        // Determine mode - DEFAULT TO UNITY
        SimulationMode mode = SimulationMode.Unity; // Default
        if (SimulationConfig.Instance != null)
        {
            mode = SimulationConfig.Instance.Mode;
        }
        
        Debug.Log($"[GameStateManager] Starting simulation in {mode} mode");
        
        if (mode == SimulationMode.Atoms)
        {
            StartAtomsSimulation();
        }
        else
        {
            StartUnitySimulation();
        }

        _inputHandler.Disable();
        ChangeState(GameState.Simulate);
    }

    private void StartUnitySimulation()
    {
        if (CombatManager.Instance == null)
        {
            Debug.LogError("[GameStateManager] CombatManager.Instance is null!");
            return;
        }
        
        CombatManager.Instance.Initialize();
        
        // Enable AI for Unity troops
        foreach (ITroop troop in _activeTroops)
        {
            Troop unityTroop = troop as Troop;
            if (unityTroop != null)
            {
                unityTroop.IsAIActive = true;
            }
        }
        
        CombatManager.Instance.AssignInitialTargets();
    }

    private void StartAtomsSimulation()
    {
        if (CombatManager_Atoms.Instance == null)
        {
            Debug.LogError("[GameStateManager] CombatManager_Atoms.Instance is null!");
            return;
        }
        
        CombatManager_Atoms.Instance.Initialize();
        
        // Just enable AI - no need to register (TeamManager already has them)
        foreach (ITroop troop in _activeTroops)
        {
            Troop_Atoms atomsTroop = troop as Troop_Atoms;
            if (atomsTroop != null)
            {
                atomsTroop.IsAIActive = true;
            }
        }
        
        CombatManager_Atoms.Instance.AssignInitialTargets();
    }

    public void ReturnToPrep()
    {
        CleanupDestroyedTroops();

        if (_currentState == GameState.Simulate)
        {
            // Disable AI for remaining troops (both types)
            foreach (ITroop troop in _activeTroops)
            {
                if (troop is Troop unityTroop && unityTroop != null && unityTroop.gameObject != null)
                {
                    unityTroop.IsAIActive = false;
                }
                else if (troop is Troop_Atoms atomsTroop && atomsTroop != null && atomsTroop.gameObject != null)
                {
                    atomsTroop.IsAIActive = false;
                }
            }
            
            ClearAllTroops();
            _saveLoadManager.RestorePrepState(_activeTroops, AvailableUnits, _unitSpawner);
            _isPaused = false;
            Time.timeScale = 1f;
        }
        else if (_currentState == GameState.Win)
        {
            // From win screen - clear everything
            ClearAllTroops();
            _isPaused = false;
            Time.timeScale = 1f;
        }

        _inputHandler.Enable();
        ChangeState(GameState.Prep);
    }

    // NEW: Return to prep from win screen without clearing layout
    public void ReturnToPrepFromWin()
    {
        if (_currentState != GameState.Win) return;

        Debug.Log("[GameStateManager] Returning to prep from win (preserving layout)");
        
        CleanupDestroyedTroops();
        ClearAllTroops();
        
        // Restore the prep state (brings back the original layout)
        _saveLoadManager.RestorePrepState(_activeTroops, AvailableUnits, _unitSpawner);
        
        _isPaused = false;
        Time.timeScale = 1f;
        
        _inputHandler.Enable();
        ChangeState(GameState.Prep);
    }

    public void RestartSimulation()
    {
        if (_currentState != GameState.Win) return;

        Debug.Log("[GameStateManager] Restarting simulation");
        
        // Start coroutine to handle destruction timing
        StartCoroutine(RestartSimulationCoroutine());
    }

    private System.Collections.IEnumerator RestartSimulationCoroutine()
    {
        Debug.Log("[GameStateManager] RestartSimulationCoroutine started");
        
        CleanupDestroyedTroops();
        ClearAllTroops();
        
        // Wait one frame for destruction to complete
        yield return null;
        Debug.Log("[GameStateManager] Destruction complete, restoring prep state");
        
        // Restore prep state
        _saveLoadManager.RestorePrepState(_activeTroops, AvailableUnits, _unitSpawner);
        
        // Determine which mode to use
        SimulationMode mode = SimulationMode.Unity;
        if (SimulationConfig.Instance != null)
        {
            mode = SimulationConfig.Instance.Mode;
        }
        
        Debug.Log($"[GameStateManager] Restored {_activeTroops.Count} troops in {mode} mode");
        
        // Enable AI for restored troops
        int enabledCount = 0;
        foreach (ITroop troop in _activeTroops)
        {
            if (troop == null)
            {
                Debug.LogWarning("[GameStateManager] Null troop in active list");
                continue;
            }
            
            if (troop is Troop unityTroop && mode == SimulationMode.Unity)
            {
                if (unityTroop.gameObject != null)
                {
                    unityTroop.IsAIActive = true;
                    enabledCount++;
                }
            }
            else if (troop is Troop_Atoms atomsTroop && mode == SimulationMode.Atoms)
            {
                if (atomsTroop.gameObject != null)
                {
                    atomsTroop.IsAIActive = true;
                    enabledCount++;
                }
            }
        }
        
        Debug.Log($"[GameStateManager] Enabled AI for {enabledCount}/{_activeTroops.Count} troops");
        
        // Wait another frame to ensure all troops are initialized
        yield return null;
        
        // Assign targets using correct manager
        if (mode == SimulationMode.Unity && CombatManager.Instance != null)
        {
            Debug.Log("[GameStateManager] Initializing Unity CombatManager");
            CombatManager.Instance.Initialize();
            CombatManager.Instance.AssignInitialTargets();
        }
        else if (mode == SimulationMode.Atoms && CombatManager_Atoms.Instance != null)
        {
            Debug.Log("[GameStateManager] Initializing Atoms CombatManager");
            CombatManager_Atoms.Instance.Initialize();
            CombatManager_Atoms.Instance.AssignInitialTargets();
        }
        else
        {
            Debug.LogError($"[GameStateManager] No combat manager found for {mode} mode!");
        }

        ChangeState(GameState.Simulate);
        Debug.Log("[GameStateManager] RestartSimulation complete");
    }

    public void TogglePause()
    {
        if (_currentState != GameState.Simulate) return;

        _isPaused = !_isPaused;
        Time.timeScale = _isPaused ? 0f : 1f;
        
        Debug.Log($"[GameStateManager] Game {(_isPaused ? "paused" : "resumed")}");
    }

    public void ClearTeam(Guid teamID)
    {
        // Deprecated - update to use index
        Debug.LogWarning("[GameStateManager] ClearTeam by GUID is deprecated, use ClearTeamByIndex");
    }

    public void ClearTeamByIndex(int teamIndex)
    {
        if (_currentState != GameState.Prep) return;

        List<ITroop> troopsToRemove = new List<ITroop>();
        foreach (ITroop troop in _activeTroops)
        {
            Troop troopMono = troop as Troop;
            if (troopMono != null && troopMono.TeamIndex == teamIndex)
            {
                troopsToRemove.Add(troop);
            }
        }

        foreach (ITroop troop in troopsToRemove)
        {
            ITeam team = FindTeamByIndex(teamIndex);
            _unitSpawner.RemoveTroop(troop, team);
        }

        OnLayoutCleared?.Invoke();
    }

    public void SaveCurrentLayout(String layoutName)
    {
        _saveLoadManager.SaveLayout(layoutName, _activeTroops);
    }

    public void LoadLayout(String filePath)
    {
        if (_currentState != GameState.Prep)
        {
            Debug.LogWarning("[GameStateManager] Can only load layouts during Prep phase");
            return;
        }

        // Clear existing troops AND team registrations
        ClearAllTroops();
        
        // Load new layout
        _saveLoadManager.LoadLayout(filePath, _activeTroops, AvailableUnits, _unitSpawner);
        
        OnLayoutCleared?.Invoke();
    }

    public List<string> GetSavedLayouts()
    {
        return _saveLoadManager.GetSavedLayouts();
    }

    // --- PRIVATE HELPERS ---

    private void OnPlaceUnitRequested(Vector3 position, TeamArea teamArea)
    {
        if (_selectedUnit == null) return;

        ITeam team = FindTeamByArea(teamArea);
        if (team != null)
        {
            _unitSpawner.SpawnTroop(_selectedUnit, position, teamArea, team);
        }
    }

    private void OnRemoveUnitRequested(ITroop troop)
    {
        Troop troopMono = troop as Troop;
        if (troopMono != null)
        {
            ITeam team = FindTeamByIndex(troopMono.TeamIndex);
            _unitSpawner.RemoveTroop(troop, team);
        }
    }

    private void OnTroopSpawned(ITroop troop)
    {
        _activeTroops.Add(troop);
    }

    private void OnTroopRemoved(ITroop troop)
    {
        if (troop != null)
        {
            _activeTroops.Remove(troop);
            Debug.Log($"[GameStateManager] Removed troop from active list. Remaining: {_activeTroops.Count}");
        }
    }

    public void ClearAllTeams()
    {
        if (_currentState != GameState.Prep) return;

        ClearAllTroops();
        OnLayoutCleared?.Invoke();
    }

    private void ClearAllTroops()
    {
        Debug.Log($"[GameStateManager] Clearing {_activeTroops.Count} troops");
        
        // Destroy all troop GameObjects (with null checks)
        int destroyedCount = 0;
        foreach (ITroop troop in _activeTroops)
        {
            if (troop != null && troop.GameObject != null) // NULL CHECK
            {
                Destroy(troop.GameObject);
                destroyedCount++;
            }
        }
        _activeTroops.Clear();

        Debug.Log($"[GameStateManager] Destroyed {destroyedCount} GameObjects");

        // Clear team registries EXPLICITLY
        if (TeamManager.Instance != null)
        {
            foreach (Team team in TeamManager.Instance.Teams)
            {
                if (team != null)
                {
                    int beforeCount = team.TotalUnits();
                    team.ClearUnits();
                    Debug.Log($"[GameStateManager] Cleared team {team.TeamIndex}: {beforeCount} -> {team.TotalUnits()} units");
                }
            }
        }
    }

    private ITeam FindTeamByArea(TeamArea area)
    {
        if (TeamManager.Instance == null) return null;
        return TeamManager.Instance.Teams.Find(t => t?.Area == area);
    }

    private ITeam FindTeamByIndex(int index)
    {
        return TeamManager.Instance?.GetTeamByIndex(index);
    }

    private void OnTeamDefeated(Team defeatedTeam)
    {
        Debug.Log($"[GameStateManager] OnTeamDefeated called for team {defeatedTeam?.TeamIndex ?? -1}");

        Team winningTeam = null;
        int activeTeams = 0;

        if (TeamManager.Instance != null)
        {
            foreach (Team team in TeamManager.Instance.Teams)
            {
                if (team != null && team.TotalUnits() > 0)
                {
                    activeTeams++;
                    winningTeam = team;
                    Debug.Log($"[GameStateManager] Team {team.TeamIndex} still has {team.TotalUnits()} units");
                }
            }
        }

        Debug.Log($"[GameStateManager] Active teams remaining: {activeTeams}");

        // Clean up dead/destroyed troops from active list
        CleanupDestroyedTroops();

        if (activeTeams == 0)
        {
            Debug.Log("[GameStateManager] DRAW - No teams remaining");
            ChangeState(GameState.Win);
            OnTeamWon?.Invoke(null);
        }
        else if (activeTeams == 1 && winningTeam != null)
        {
            Debug.Log($"[GameStateManager] Team {winningTeam.TeamIndex} WINS with {winningTeam.TotalUnits()} units");
            ChangeState(GameState.Win);
            OnTeamWon?.Invoke(winningTeam);
        }
        else
        {
            Debug.Log($"[GameStateManager] Combat continues - {activeTeams} teams still active");
        }
    }

   
    private void CleanupDestroyedTroops()
    {
        int beforeCount = _activeTroops.Count;
        
        _activeTroops.RemoveAll(troop =>
        {
            // Unity's == operator handles destroyed objects
            if (troop == null) return true;
            
            // Check Unity Troop
            if (troop is Troop unityTroop)
            {
                if (unityTroop == null || unityTroop.IsDead) return true;
            }
            // Check Atoms Troop
            else if (troop is Troop_Atoms atomsTroop)
            {
                if (atomsTroop == null || atomsTroop.IsDead) return true;
            }
            
            return false;
        });
        
        int removed = beforeCount - _activeTroops.Count;
        if (removed > 0)
        {
            Debug.Log($"[GameStateManager] Cleaned up {removed} destroyed troops. Remaining: {_activeTroops.Count}");
        }
    }

    private void ChangeState(GameState newState)
    {
        _currentState = newState;
        OnStateChanged?.Invoke(newState);
    }



    void Update()
    {
        // Periodic cleanup of destroyed troops (every 5 seconds)
        if (_currentState == GameState.Simulate && Time.frameCount % 300 == 0)
        {
            CleanupDestroyedTroops();
        }
    }
}