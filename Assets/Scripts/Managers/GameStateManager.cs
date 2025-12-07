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
    public event Action<bool> OnPauseChanged;
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

        // Enable AI for all troops
        foreach (ITroop troop in _activeTroops)
        {
            Troop troopMono = troop as Troop;
            if (troopMono != null)
            {
                troopMono.IsAIActive = true;
            }
        }

        // Assign targets immediately
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.AssignInitialTargets();
        }

        _inputHandler.Disable();
        ChangeState(GameState.Simulate);
    }

    public void ReturnToPrep()
    {
        // NEW: Clean up destroyed references first
        CleanupDestroyedTroops();

        if (_currentState == GameState.Simulate)
        {
            // Disable AI for remaining troops
            foreach (ITroop troop in _activeTroops)
            {
                Troop troopMono = troop as Troop;
                if (troopMono != null && troopMono.gameObject != null) // NULL CHECK
                {
                    troopMono.IsAIActive = false;
                }
            }
            
            ClearAllTroops();
            _saveLoadManager.RestorePrepState(_activeTroops, AvailableUnits, _unitSpawner);
            _isPaused = false;
            Time.timeScale = 1f;
        }
        else if (_currentState == GameState.Win)
        {
            ClearAllTroops();
        }

        _inputHandler.Enable();
        ChangeState(GameState.Prep);
    }

    public void RestartSimulation()
    {
        if (_currentState != GameState.Win) return;

        Debug.Log("[GameStateManager] Restarting simulation");
        
        // Clean up any destroyed references
        CleanupDestroyedTroops();

        // Destroy remaining troops
        ClearAllTroops();
        
        // Restore from snapshot
        _saveLoadManager.RestorePrepState(_activeTroops, AvailableUnits, _unitSpawner);
        
        // Enable AI for ALL restored troops
        int enabledCount = 0;
        foreach (ITroop troop in _activeTroops)
        {
            Troop troopMono = troop as Troop;
            if (troopMono != null && troopMono.gameObject != null) // NULL CHECK
            {
                troopMono.IsAIActive = true;
                enabledCount++;
            }
        }
        
        Debug.Log($"[GameStateManager] Enabled AI for {enabledCount} troops");
        
        // Assign targets
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.AssignInitialTargets();
        }

        ChangeState(GameState.Simulate);
    }

    public void TogglePause()
    {
        if (_currentState != GameState.Simulate) return;

        _isPaused = !_isPaused;
        Time.timeScale = _isPaused ? 0f : 1f;
        OnPauseChanged?.Invoke(_isPaused);
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

    public void SaveCurrentLayout(string layoutName)
    {
        _saveLoadManager.SaveLayout(layoutName, _activeTroops);
    }

    public void LoadLayout(string filePath)
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

    private ITeam FindTeamByID(Guid id)
    {
        // Deprecated - use FindTeamByIndex instead
        return null;
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
            
            // Cast to MonoBehaviour to use Unity's null check
            Troop troopMono = troop as Troop;
            if (troopMono == null) return true; // Handles destroyed case
            
            // Check IsDead
            if (troopMono.IsDead) return true;
            
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