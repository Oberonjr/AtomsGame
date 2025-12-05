using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameStateManager : MonoBehaviour
{
    private static GameStateManager _instance;
    public static GameStateManager Instance => _instance;

    [Header("Unit Selection")]
    public List<UnitSelectionData> AvailableUnits = new List<UnitSelectionData>();

    [Header("References")]
    public LayerMask GroundLayer;
    public LayerMask UILayer;

    [Header("Save System")]
    public string SaveDirectory = "BattleLayouts";

    private GameState _currentState = GameState.Prep;
    private UnitSelectionData _selectedUnit;
    private List<TroopSnapshot> _prepStateSnapshot = new List<TroopSnapshot>();
    private List<Troop> _activeTroops = new List<Troop>();
    private bool _isPaused = false;

    public GameState CurrentState => _currentState;
    public UnitSelectionData SelectedUnit => _selectedUnit;
    public bool IsPaused => _isPaused;

    public event Action<GameState> OnStateChanged;
    public event Action<UnitSelectionData> OnUnitSelected;
    public event Action<Team> OnTeamWon;
    public event Action<bool> OnPauseChanged;
    public event Action OnLayoutCleared; // New event for UI updates

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            Debug.Log("[GameStateManager] Instance created");
        }
        else
        {
            Debug.LogWarning("[GameStateManager] Duplicate instance destroyed");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Log camera setup for orthographic
        if (Camera.main != null)
        {
            Debug.Log($"[GameStateManager] Camera: {Camera.main.name}");
            Debug.Log($"[GameStateManager] Camera position: {Camera.main.transform.position}");
            Debug.Log($"[GameStateManager] Camera rotation: {Camera.main.transform.eulerAngles}");
            Debug.Log($"[GameStateManager] Camera projection: {(Camera.main.orthographic ? "Orthographic" : "Perspective")}");
            if (Camera.main.orthographic)
            {
                Debug.Log($"[GameStateManager] Orthographic size: {Camera.main.orthographicSize}");
            }
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

    void Update()
    {
        if (_currentState == GameState.Prep)
        {
            HandlePrepInput();
        }
    }

    public void SelectUnit(UnitSelectionData unit)
    {
        _selectedUnit = unit;
        Debug.Log($"[GameStateManager] Unit selected: {(unit != null ? unit.DisplayName : "None")}");
        OnUnitSelected?.Invoke(unit);
    }

    public void DeselectUnit()
    {
        _selectedUnit = null;
        Debug.Log("[GameStateManager] Unit deselected");
        OnUnitSelected?.Invoke(null);
    }

    private bool IsPointerOverUI()
    {
        // Simple raycast check - if it hits UI layer, block
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, UILayer))
        {
            Debug.Log($"[GameStateManager] Click blocked - hit UI: {hit.collider.name}");
            return true;
        }
        
        return false;
    }

    private void HandlePrepInput()
    {
        if (Input.GetMouseButtonDown(0)) // Left click - place unit
        {
            // Check if clicking on UI first
            if (IsPointerOverUI())
            {
                return;
            }
            
            Debug.Log($"[GameStateManager] Left click detected. Selected unit: {(_selectedUnit != null ? _selectedUnit.DisplayName : "None")}");

            if (_selectedUnit != null)
            {
                TryPlaceUnit();
            }
            else
            {
                Debug.LogWarning("[GameStateManager] Cannot place unit - no unit selected");
            }
        }
        else if (Input.GetMouseButtonDown(1)) // Right click - remove unit
        {
            // Check if clicking on UI first
            if (IsPointerOverUI())
            {
                return;
            }
            
            Debug.Log("[GameStateManager] Right click detected");
            TryRemoveUnit();
        }
    }

    private void TryPlaceUnit()
    {
        if (Camera.main == null)
        {
            Debug.LogError("[GameStateManager] Camera.main is null! Make sure your camera is tagged 'MainCamera'");
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, GroundLayer))
        {
            Debug.Log($"[GameStateManager] Raycast HIT! Position: {hit.point}, collider: {hit.collider.name}");
            
            TeamArea teamArea = FindTeamAreaAtPosition(hit.point);
            if (teamArea != null)
            {
                Debug.Log($"[GameStateManager] Found team area at position");
                SpawnTroop(_selectedUnit, hit.point, teamArea);
            }
            else
            {
                Debug.LogWarning($"[GameStateManager] No team area found at position: {hit.point}");
            }
        }
        else
        {
            Debug.LogWarning($"[GameStateManager] Raycast missed ground");
        }
    }

    private void TryRemoveUnit()
    {
        if (Camera.main == null)
        {
            Debug.LogError("[GameStateManager] Camera.main is null!");
            return;
        }

        // Use 2D raycast for 2D colliders
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity);
        
        if (hit.collider != null)
        {
            Debug.Log($"[GameStateManager] Right click hit: {hit.collider.name}");
            
            Troop troop = hit.collider.GetComponentInParent<Troop>();
            if (troop != null)
            {
                Debug.Log($"[GameStateManager] Found troop to remove: {troop.name}");
                RemoveTroop(troop);
            }
            else
            {
                Debug.LogWarning($"[GameStateManager] No Troop component on: {hit.collider.name}");
            }
        }
        else
        {
            Debug.LogWarning("[GameStateManager] Right click raycast missed");
        }
    }

    private TeamArea FindTeamAreaAtPosition(Vector3 position)
    {
        if (TeamManager.Instance == null)
        {
            Debug.LogError("[GameStateManager] TeamManager.Instance is null!");
            return null;
        }

        foreach (Team team in TeamManager.Instance.Teams)
        {
            if (team?.Area != null)
            {
                float distance = Vector3.Distance(team.Area.transform.position, position);
                
                if (distance <= team.Area.Radius)
                {
                    Debug.Log($"[GameStateManager] Found matching team area!");
                    return team.Area;
                }
            }
        }
        return null;
    }

    private void SpawnTroop(UnitSelectionData unitData, Vector3 position, TeamArea teamArea)
    {
        if (unitData?.TroopPrefab == null)
        {
            Debug.LogError("[GameStateManager] Cannot spawn - unitData or TroopPrefab is null!");
            return;
        }

        // For 2D XY plane, ensure Z is 0
        position.z = 0f;

        // Sample the NavMesh to find the nearest valid position
        if (UnityEngine.AI.NavMesh.SamplePosition(position, out UnityEngine.AI.NavMeshHit navHit, 2f, UnityEngine.AI.NavMesh.AllAreas))
        {
            position = navHit.position;
            position.z = 0f;
        }

        // Calculate spawn rotation to face enemies
        Quaternion spawnRotation = CalculateSpawnRotation(teamArea);

        Debug.Log($"[GameStateManager] Spawning {unitData.DisplayName} at {position}");

        Troop troop = Instantiate(unitData.TroopPrefab, position, spawnRotation);

        if (troop != null)
        {
            if (troop.TroopStats == null)
            {
                Debug.LogError($"[GameStateManager] TroopStats is null on prefab!");
            }

            // Ensure position and rotation are correct
            Vector3 finalPos = troop.transform.position;
            finalPos.z = 0f;
            troop.transform.position = finalPos;
            troop.transform.rotation = spawnRotation;

            troop.enabled = false; // Disable during prep
            Debug.Log($"[GameStateManager] Troop disabled for prep");

            Team owningTeam = FindTeamByArea(teamArea);
            if (owningTeam != null)
            {
                troop.TeamID = owningTeam.ID;
                owningTeam.RegisterUnit(troop);
                _activeTroops.Add(troop);
                Debug.Log($"[GameStateManager] Registered to team. Total troops: {_activeTroops.Count}");
            }
            else
            {
                Debug.LogError("[GameStateManager] Could not find owning team!");
            }
        }
        else
        {
            Debug.LogError("[GameStateManager] Failed to instantiate troop!");
        }
    }   

    private Quaternion CalculateSpawnRotation(TeamArea spawnArea)
    {
        if (TeamManager.Instance == null || spawnArea == null)
        {
            return Quaternion.identity;
        }

        Vector3 enemyDirection = Vector3.zero;
        int enemyCount = 0;

        foreach (Team team in TeamManager.Instance.Teams)
        {
            if (team?.Area != null && team.Area != spawnArea)
            {
                Vector3 direction = (team.Area.transform.position - spawnArea.transform.position).normalized;
                enemyDirection += direction;
                enemyCount++;
            }
        }

        if (enemyCount > 0)
        {
            enemyDirection /= enemyCount;
            float angle = Mathf.Atan2(enemyDirection.y, enemyDirection.x) * Mathf.Rad2Deg;
            return Quaternion.Euler(0, 0, angle);
        }

        return Quaternion.identity;
    }

    private void RemoveTroop(Troop troop)
    {
        if (troop == null)
        {
            Debug.LogWarning("[GameStateManager] Cannot remove null troop");
            return;
        }

        Debug.Log($"[GameStateManager] Removing troop: {troop.name}");

        Team owningTeam = FindTeamByID(troop.TeamID);
        if (owningTeam != null)
        {
            owningTeam.OnUnitDied(troop);
        }

        _activeTroops.Remove(troop);
        Destroy(troop.gameObject);
        Debug.Log($"[GameStateManager] Removed. Remaining: {_activeTroops.Count}");
    }

    private Team FindTeamByArea(TeamArea area)
    {
        if (TeamManager.Instance == null)
        {
            Debug.LogError("[GameStateManager] TeamManager.Instance is null!");
            return null;
        }
        
        Team foundTeam = TeamManager.Instance.Teams.Find(t => t?.Area == area);
        if (foundTeam == null)
        {
            Debug.LogWarning("[GameStateManager] Could not find team for area");
        }
        return foundTeam;
    }

    private Team FindTeamByID(Guid id)
    {
        if (TeamManager.Instance == null)
        {
            Debug.LogError("[GameStateManager] TeamManager.Instance is null!");
            return null;
        }
        
        return TeamManager.Instance.Teams.Find(t => t?.ID == id);
    }

    public void StartSimulation()
    {
        if (_currentState != GameState.Prep)
        {
            Debug.LogWarning($"[GameStateManager] Cannot start - not in Prep (current: {_currentState})");
            return;
        }

        Debug.Log("[GameStateManager] Starting simulation...");
        SavePrepState();

        foreach (Troop troop in _activeTroops)
        {
            if (troop != null)
            {
                troop.enabled = true;
            }
        }

        Debug.Log($"[GameStateManager] Enabled {_activeTroops.Count} troops");
        ChangeState(GameState.Simulate);
    }

    public void ReturnToPrep()
    {
        Debug.Log($"[GameStateManager] Returning to prep from {_currentState}");
        
        if (_currentState == GameState.Simulate)
        {
            foreach (Troop troop in _activeTroops)
            {
                if (troop != null) Destroy(troop.gameObject);
            }
            _activeTroops.Clear();

            if (TeamManager.Instance != null)
            {
                foreach (Team team in TeamManager.Instance.Teams)
                {
                    team?.ClearUnits();
                }
            }

            RestorePrepState();
            _isPaused = false;
            Time.timeScale = 1f;
        }
        else if (_currentState == GameState.Win)
        {
            foreach (Troop troop in _activeTroops)
            {
                if (troop != null) Destroy(troop.gameObject);
            }
            _activeTroops.Clear();
            _prepStateSnapshot.Clear();

            if (TeamManager.Instance != null)
            {
                foreach (Team team in TeamManager.Instance.Teams)
                {
                    team?.ClearUnits();
                }
            }
        }

        ChangeState(GameState.Prep);
    }

    public void RestartSimulation()
    {
        Debug.Log($"[GameStateManager] Restart requested from state: {_currentState}");
        
        if (_currentState != GameState.Win)
        {
            Debug.LogWarning($"[GameStateManager] Cannot restart - not in Win (current: {_currentState})");
            return;
        }

        Debug.Log("[GameStateManager] Restarting simulation...");

        foreach (Troop troop in _activeTroops)
        {
            if (troop != null) Destroy(troop.gameObject);
        }
        _activeTroops.Clear();

        if (TeamManager.Instance != null)
        {
            foreach (Team team in TeamManager.Instance.Teams)
            {
                team?.ClearUnits();
            }
        }

        RestorePrepState();
        
        Debug.Log("[GameStateManager] Transitioning to Simulate state...");
        ChangeState(GameState.Simulate);
        
        foreach (Troop troop in _activeTroops)
        {
            if (troop != null)
            {
                troop.enabled = true;
            }
        }
        
        Debug.Log($"[GameStateManager] Restart complete with {_activeTroops.Count} troops");
    }

    public void TogglePause()
    {
        if (_currentState != GameState.Simulate)
        {
            Debug.LogWarning($"[GameStateManager] Cannot pause - not in Simulate (current: {_currentState})");
            return;
        }

        _isPaused = !_isPaused;
        Time.timeScale = _isPaused ? 0f : 1f;
        Debug.Log($"[GameStateManager] Game {(_isPaused ? "paused" : "resumed")}");
        OnPauseChanged?.Invoke(_isPaused);
    }

    private void SavePrepState()
    {
        _prepStateSnapshot.Clear();
        foreach (Troop troop in _activeTroops)
        {
            if (troop != null)
            {
                _prepStateSnapshot.Add(new TroopSnapshot(troop));
            }
        }
        Debug.Log($"[GameStateManager] Saved {_prepStateSnapshot.Count} troops");
    }

    private void RestorePrepState()
    {
        Debug.Log($"[GameStateManager] Restoring {_prepStateSnapshot.Count} troops");
        
        foreach (TroopSnapshot snapshot in _prepStateSnapshot)
        {
            UnitSelectionData unitData = AvailableUnits.Find(u => 
                u?.TroopPrefab != null && u.TroopPrefab.TroopStats == snapshot.Stats);
            
            if (unitData?.TroopPrefab != null)
            {
                Troop troop = Instantiate(unitData.TroopPrefab, snapshot.Position, snapshot.Rotation);

                if (troop != null)
                {
                    troop.TeamID = snapshot.TeamID;
                    troop.CurrentHealth = snapshot.CurrentHealth;
                    troop.enabled = false;

                    Team owningTeam = FindTeamByID(snapshot.TeamID);
                    if (owningTeam != null)
                    {
                        owningTeam.RegisterUnit(troop);
                    }

                    _activeTroops.Add(troop);
                }
            }
            else
            {
                Debug.LogError($"[GameStateManager] Could not find UnitSelectionData for: {snapshot.Stats?.name}");
            }
        }
        
        Debug.Log($"[GameStateManager] Restored {_activeTroops.Count} troops");
    }

    #region Clear Teams

    public void ClearTeam(Guid teamID)
    {
        if (_currentState != GameState.Prep)
        {
            Debug.LogWarning("[GameStateManager] Can only clear teams during Prep phase");
            return;
        }

        List<Troop> troopsToRemove = new List<Troop>();

        // Find all troops belonging to this team
        foreach (Troop troop in _activeTroops)
        {
            if (troop != null && troop.TeamID == teamID)
            {
                troopsToRemove.Add(troop);
            }
        }

        // Remove them
        foreach (Troop troop in troopsToRemove)
        {
            RemoveTroop(troop);
        }

        Debug.Log($"[GameStateManager] Cleared team {teamID}, removed {troopsToRemove.Count} troops");
        OnLayoutCleared?.Invoke();
    }

    public void ClearAllTeams()
    {
        if (_currentState != GameState.Prep)
        {
            Debug.LogWarning("[GameStateManager] Can only clear teams during Prep phase");
            return;
        }

        int count = _activeTroops.Count;

        // Destroy all troops
        foreach (Troop troop in _activeTroops)
        {
            if (troop != null)
            {
                Destroy(troop.gameObject);
            }
        }

        _activeTroops.Clear();

        // Clear team registries
        if (TeamManager.Instance != null)
        {
            foreach (Team team in TeamManager.Instance.Teams)
            {
                team?.ClearUnits();
            }
        }

        Debug.Log($"[GameStateManager] Cleared all teams, removed {count} troops");
        OnLayoutCleared?.Invoke();
    }

    #endregion

    #region Save/Load Layout

    public void SaveCurrentLayout(string layoutName)
    {
        if (_currentState != GameState.Prep)
        {
            Debug.LogWarning("[GameStateManager] Can only save layouts during Prep phase");
            return;
        }

        BattleLayout layout = new BattleLayout
        {
            LayoutName = layoutName,
            DateCreated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        // Save all active troops
        foreach (Troop troop in _activeTroops)
        {
            if (troop == null || troop.TroopStats == null) continue;

            SavedTroop savedTroop = new SavedTroop
            {
                TroopStatsName = troop.TroopStats.name,
                Position = troop.transform.position,
                Rotation = troop.transform.rotation,
                TeamID = troop.TeamID.ToString(),
                CurrentHealth = troop.CurrentHealth
            };

            layout.SavedTroops.Add(savedTroop);
        }

        // Create save directory if it doesn't exist
        string saveDir = Path.Combine(Application.persistentDataPath, SaveDirectory);
        if (!Directory.Exists(saveDir))
        {
            Directory.CreateDirectory(saveDir);
        }

        // Save to JSON
        string fileName = $"{layoutName}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        string filePath = Path.Combine(saveDir, fileName);
        string json = JsonUtility.ToJson(layout, true);
        File.WriteAllText(filePath, json);

        Debug.Log($"[GameStateManager] Saved layout '{layoutName}' with {layout.SavedTroops.Count} troops to: {filePath}");
    }

    public void LoadLayout(string filePath)
    {
        if (_currentState != GameState.Prep)
        {
            Debug.LogWarning("[GameStateManager] Can only load layouts during Prep phase");
            return;
        }

        if (!File.Exists(filePath))
        {
            Debug.LogError($"[GameStateManager] Layout file not found: {filePath}");
            return;
        }

        // Clear existing troops first
        ClearAllTeams();

        // Load JSON
        string json = File.ReadAllText(filePath);
        BattleLayout layout = JsonUtility.FromJson<BattleLayout>(json);

        if (layout == null)
        {
            Debug.LogError("[GameStateManager] Failed to parse layout file");
            return;
        }

        Debug.Log($"[GameStateManager] Loading layout '{layout.LayoutName}' with {layout.SavedTroops.Count} troops");

        // Spawn troops
        foreach (SavedTroop savedTroop in layout.SavedTroops)
        {
            // Find matching TroopStats
            UnitSelectionData unitData = AvailableUnits.Find(u =>
                u?.TroopPrefab != null &&
                u.TroopPrefab.TroopStats != null &&
                u.TroopPrefab.TroopStats.name == savedTroop.TroopStatsName);

            if (unitData == null)
            {
                Debug.LogWarning($"[GameStateManager] Could not find unit data for: {savedTroop.TroopStatsName}");
                continue;
            }

            // Instantiate troop
            Troop troop = Instantiate(unitData.TroopPrefab, savedTroop.Position, savedTroop.Rotation);

            if (troop != null)
            {
                troop.TeamID = Guid.Parse(savedTroop.TeamID);
                troop.CurrentHealth = savedTroop.CurrentHealth;
                troop.enabled = false; // Disable during prep

                // Register to team
                Team owningTeam = FindTeamByID(troop.TeamID);
                if (owningTeam != null)
                {
                    owningTeam.RegisterUnit(troop);
                }

                _activeTroops.Add(troop);
            }
        }

        Debug.Log($"[GameStateManager] Loaded {_activeTroops.Count} troops from layout '{layout.LayoutName}'");
        OnLayoutCleared?.Invoke(); // Trigger UI update
    }

    public List<string> GetSavedLayouts()
    {
        string saveDir = Path.Combine(Application.persistentDataPath, SaveDirectory);
        if (!Directory.Exists(saveDir))
        {
            return new List<string>();
        }

        string[] files = Directory.GetFiles(saveDir, "*.json");
        return new List<string>(files);
    }

    #endregion

    private void OnTeamDefeated(Team defeatedTeam)
    {
        Debug.Log($"[GameStateManager] Team defeated! Current state: {_currentState}");

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
                }
            }
        }

        // Check for draw (0 teams remaining) or win (1 team remaining)
        if (activeTeams == 0)
        {
            Debug.Log("[GameStateManager] Draw - no teams remaining!");
            ChangeState(GameState.Win);
            OnTeamWon?.Invoke(null); // Pass null to indicate draw
        }
        else if (activeTeams == 1 && winningTeam != null)
        {
            Debug.Log($"[GameStateManager] Winner found with {winningTeam.TotalUnits()} units");
            ChangeState(GameState.Win);
            OnTeamWon?.Invoke(winningTeam);
        }
    }

    private void ChangeState(GameState newState)
    {
        Debug.Log($"[GameStateManager] State: {_currentState} -> {newState}");
        _currentState = newState;
        OnStateChanged?.Invoke(newState);
    }
}