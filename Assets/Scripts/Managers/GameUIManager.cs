using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class GameUIManager : MonoBehaviour
{
    [Header("Prep UI")]
    public GameObject PrepUI;
    public Transform UnitButtonContainer;
    public GameObject UnitButtonPrefab;
    public Button SimulateButton;

    [Header("Simulate UI")]
    public GameObject SimulateUI;
    public Button BackToPrepButton;
    public TextMeshProUGUI SimulateStatusText; // Optional: show "Paused" text

    [Header("Pause Menu UI")]
    public GameObject PauseMenuPanel;
    public Button ContinueButton;
    public Button MainMenuButton;
    public Button QuitButton;

    [Header("Win UI")]
    public GameObject WinUI;
    public TextMeshProUGUI WinnerText;
    public Image WinnerColorDisplay;
    public Button RestartButton;
    public Button BackToPrepFromWinButton;

    [Header("Prep UI - Team Management")]
    public Button ClearAllButton;
    public Transform ClearTeamButtonContainer;
    public GameObject ClearTeamButtonPrefab;

    [Header("Prep UI - Save/Load")]
    public Button SaveLayoutButton;
    public Button LoadLayoutButton;
    public TMP_InputField SaveNameInput;
    public GameObject SaveLayoutPanel;
    public Button ConfirmSaveButton;
    public GameObject LoadLayoutPanel;
    public Transform LoadLayoutButtonContainer;
    public GameObject LoadLayoutButtonPrefab;

    [Header("Confirmation Dialog")]
    public GameObject ConfirmDeletePanel;
    public TextMeshProUGUI ConfirmDeleteText;
    public Button ConfirmDeleteYesButton;
    public Button ConfirmDeleteNoButton;

    [Header("Scene Settings")]
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    private List<Button> _unitButtons = new List<Button>();
    private List<Button> _clearTeamButtons = new List<Button>();
    private static GameUIManager _instance;
    public static GameUIManager Instance => _instance;
    private string _fileToDelete = null;
    private bool _wasSimulationPausedBeforeMenu = false;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log("[GameUIManager] Initializing...");
        
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged += OnStateChanged;
            GameStateManager.Instance.OnUnitSelected += OnUnitSelected;
            GameStateManager.Instance.OnTeamWon += OnTeamWon;

            CreateUnitButtons();
            CreateClearTeamButtons();
            SetupButtons();
            OnStateChanged(GameStateManager.Instance.CurrentState);
            
            // Ensure pause menu is hidden initially
            if (PauseMenuPanel != null)
            {
                PauseMenuPanel.SetActive(false);
            }
            
            Debug.Log("[GameUIManager] Initialization complete");
        }
        else
        {
            Debug.LogError("[GameUIManager] GameStateManager.Instance is null!");
        }
    }

    void OnDestroy()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= OnStateChanged;
            GameStateManager.Instance.OnUnitSelected -= OnUnitSelected;
            GameStateManager.Instance.OnTeamWon -= OnTeamWon;
        }
    }

    private void CreateUnitButtons()
    {
        if (GameStateManager.Instance == null || UnitButtonContainer == null || UnitButtonPrefab == null)
        {
            Debug.LogError($"[GameUIManager] Cannot create unit buttons - missing references");
            return;
        }

        foreach (var unitData in GameStateManager.Instance.AvailableUnits)
        {
            if (unitData == null) continue;
            
            GameObject buttonObj = Instantiate(UnitButtonPrefab, UnitButtonContainer);
            Button button = buttonObj.GetComponent<Button>();

            if (button != null)
            {
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = unitData.DisplayName;
                }

                Image buttonImage = buttonObj.transform.GetChild(0)?.GetComponent<Image>();
                if (buttonImage == null)
                {
                    buttonImage = buttonObj.GetComponentInChildren<Image>();
                }
                
                if (buttonImage != null && unitData.Icon != null)
                {
                    buttonImage.sprite = unitData.Icon;
                }

                UnitSelectionData capturedData = unitData;
                button.onClick.AddListener(() =>
                {
                    GameStateManager.Instance.SelectUnit(capturedData);
                });

                _unitButtons.Add(button);
            }
        }
    }

    private void CreateClearTeamButtons()
    {
        if (TeamManager.Instance == null || ClearTeamButtonContainer == null || ClearTeamButtonPrefab == null)
        {
            Debug.LogWarning("[GameUIManager] Cannot create clear team buttons - missing references");
            return;
        }

        for (int i = 0; i < TeamManager.Instance.Teams.Count; i++)
        {
            Team team = TeamManager.Instance.Teams[i];
            if (team == null) continue;

            GameObject buttonObj = Instantiate(ClearTeamButtonPrefab, ClearTeamButtonContainer);
            Button button = buttonObj.GetComponent<Button>();

            if (button != null)
            {
                TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = $"Clear Team {i + 1}";
                }

                Image buttonImage = button.GetComponent<Image>();
                if (buttonImage != null)
                {
                    Color color = team.TeamColor;
                    color.a = 0.5f;
                    buttonImage.color = color;
                }

                int teamIndex = i;
                button.onClick.AddListener(() => OnClearTeamClicked(teamIndex));

                _clearTeamButtons.Add(button);
            }
        }
    }

    private void SetupButtons()
    {
        // Prep buttons
        if (SimulateButton != null)
            SimulateButton.onClick.AddListener(() => GameStateManager.Instance.StartSimulation());

        // Simulate buttons
        if (BackToPrepButton != null)
            BackToPrepButton.onClick.AddListener(() => GameStateManager.Instance.ReturnToPrep());

        // Pause Menu buttons
        if (ContinueButton != null)
            ContinueButton.onClick.AddListener(OnContinueClicked);

        if (MainMenuButton != null)
            MainMenuButton.onClick.AddListener(OnMainMenuClicked);

        if (QuitButton != null)
            QuitButton.onClick.AddListener(OnQuitClicked);

        // Win buttons
        if (RestartButton != null)
            RestartButton.onClick.AddListener(() => GameStateManager.Instance.RestartSimulation());

        if (BackToPrepFromWinButton != null)
            BackToPrepFromWinButton.onClick.AddListener(() => GameStateManager.Instance.ReturnToPrepFromWin());

        // Team management buttons
        if (ClearAllButton != null)
            ClearAllButton.onClick.AddListener(OnClearAllClicked);

        // Save/load buttons
        if (SaveLayoutButton != null)
            SaveLayoutButton.onClick.AddListener(OnSaveLayoutClicked);

        if (ConfirmSaveButton != null)
            ConfirmSaveButton.onClick.AddListener(OnConfirmSaveClicked);

        if (LoadLayoutButton != null)
            LoadLayoutButton.onClick.AddListener(OnLoadLayoutClicked);

        // Delete confirmation buttons
        if (ConfirmDeleteYesButton != null)
            ConfirmDeleteYesButton.onClick.AddListener(OnConfirmDeleteYes);

        if (ConfirmDeleteNoButton != null)
            ConfirmDeleteNoButton.onClick.AddListener(OnConfirmDeleteNo);
    }

    private void OnStateChanged(GameState newState)
    {
        Debug.Log($"[GameUIManager] State changed to: {newState}");
        
        if (PrepUI != null) PrepUI.SetActive(newState == GameState.Prep);
        if (SimulateUI != null) SimulateUI.SetActive(newState == GameState.Simulate);
        if (WinUI != null) WinUI.SetActive(newState == GameState.Win);

        // Close all panels when changing states
        if (LoadLayoutPanel != null) LoadLayoutPanel.SetActive(false);
        if (SaveLayoutPanel != null) SaveLayoutPanel.SetActive(false);
        if (PauseMenuPanel != null) PauseMenuPanel.SetActive(false);
    }

    private void OnUnitSelected(UnitSelectionData unit)
    {
        // Visual feedback for selected unit
        for (int i = 0; i < _unitButtons.Count; i++)
        {
            Button button = _unitButtons[i];
            if (button != null)
            {
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                button.colors = colors;
                
                Image buttonImage = button.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = Color.white;
                }
            }
        }

        if (unit != null)
        {
            int index = GameStateManager.Instance.AvailableUnits.IndexOf(unit);
            
            if (index >= 0 && index < _unitButtons.Count)
            {
                Button selectedButton = _unitButtons[index];
                ColorBlock colors = selectedButton.colors;
                colors.normalColor = Color.yellow;
                colors.selectedColor = Color.yellow;
                selectedButton.colors = colors;
                
                Image buttonImage = selectedButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = Color.yellow;
                }
            }
        }
    }

    private void OnTeamWon(Team winner)
    {
        Debug.Log("[GameUIManager] Team won!");

        if (WinnerText != null)
        {
            if (winner == null)
            {
                WinnerText.text = "Draw!";
                WinnerText.color = Color.white;
                if (WinnerColorDisplay != null)
                {
                    WinnerColorDisplay.color = Color.gray;
                }
            }
            else
            {
                int teamIndex = TeamManager.Instance != null ? TeamManager.Instance.Teams.IndexOf(winner) : -1;
                string teamName = teamIndex >= 0 ? $"Team {teamIndex + 1}" : "Team";
                
                WinnerText.text = $"{teamName} Wins!";
                WinnerText.color = winner.TeamColor;
                if (WinnerColorDisplay != null)
                {
                    WinnerColorDisplay.color = winner.TeamColor;
                }
            }
        }
    }

    // PAUSE MENU HANDLERS
    public void ShowPauseMenu()
    {
        if (PauseMenuPanel == null) return;

        // Remember current game state for proper resume behavior
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState == GameState.Simulate)
        {
            _wasSimulationPausedBeforeMenu = GameStateManager.Instance.IsPaused;
        }
        else
        {
            // Not in simulate state - don't need to track pause
            _wasSimulationPausedBeforeMenu = false;
        }

        // Freeze game
        Time.timeScale = 0f;
        
        PauseMenuPanel.SetActive(true);
        
        Debug.Log($"[GameUIManager] Pause menu shown in {GameStateManager.Instance?.CurrentState} state (was paused: {_wasSimulationPausedBeforeMenu})");
    }

    public void HidePauseMenu()
    {
        if (PauseMenuPanel == null) return;

        PauseMenuPanel.SetActive(false);

        // Only resume time if:
        // 1. We're in simulate state AND simulation wasn't paused before menu
        // 2. OR we're in any other state (prep, win) - just resume normally
        if (GameStateManager.Instance != null)
        {
            if (GameStateManager.Instance.CurrentState == GameState.Simulate)
            {
                // Only resume if wasn't paused before
                if (!_wasSimulationPausedBeforeMenu)
                {
                    Time.timeScale = 1f;
                }
            }
            else
            {
                // Always resume in other states
                Time.timeScale = 1f;
            }
        }
        else
        {
            // Fallback - just resume
            Time.timeScale = 1f;
        }

        Debug.Log($"[GameUIManager] Pause menu hidden (timescale: {Time.timeScale})");
    }

    public bool IsPauseMenuActive()
    {
        return PauseMenuPanel != null && PauseMenuPanel.activeSelf;
    }

    private void OnContinueClicked()
    {
        Debug.Log("[GameUIManager] Continue clicked");
        HidePauseMenu();
    }

    private void OnMainMenuClicked()
    {
        Debug.Log("[GameUIManager] Main Menu clicked");
        
        // Unpause game time
        Time.timeScale = 1f;
        
        // Destroy SimulationConfig so user can select mode again
        if (SimulationConfig.Instance != null)
        {
            Destroy(SimulationConfig.Instance.gameObject);
        }
        
        // Load main menu scene
        SceneManager.LoadScene(_mainMenuSceneName);
    }

    private void OnQuitClicked()
    {
        Debug.Log("[GameUIManager] Quit clicked");
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // OTHER BUTTON HANDLERS
    private void OnClearTeamClicked(int teamIndex)
    {
        Debug.Log($"[GameUIManager] Clear team button clicked: Team {teamIndex}");
        GameStateManager.Instance?.ClearTeamByIndex(teamIndex);
    }

    private void OnClearAllClicked()
    {
        Debug.Log("[GameUIManager] Clear all button clicked");
        GameStateManager.Instance?.ClearAllTeams();
    }

    private void OnSaveLayoutClicked()
    {
        Debug.Log("[GameUIManager] Save layout button clicked");
        
        if (SaveLayoutPanel != null)
        {
            SaveLayoutPanel.SetActive(!SaveLayoutPanel.activeSelf);
            
            if (SaveLayoutPanel.activeSelf && SaveNameInput != null)
            {
                SaveNameInput.text = "";
            }
        }
    }

    private void OnConfirmSaveClicked()
    {
        string layoutName = SaveNameInput != null && !string.IsNullOrEmpty(SaveNameInput.text)
            ? SaveNameInput.text
            : $"Layout_{System.DateTime.Now:yyMMdd_HHmmss}";

        Debug.Log($"[GameUIManager] Confirm save clicked: {layoutName}");
        GameStateManager.Instance?.SaveCurrentLayout(layoutName);

        if (SaveLayoutPanel != null)
        {
            SaveLayoutPanel.SetActive(false);
        }
        if (SaveNameInput != null)
        {
            SaveNameInput.text = "";
        }
    }

    private void OnLoadLayoutClicked()
    {
        Debug.Log("[GameUIManager] Load layout button clicked");
        
        if (LoadLayoutPanel != null)
        {
            LoadLayoutPanel.SetActive(!LoadLayoutPanel.activeSelf);
            
            if (LoadLayoutPanel.activeSelf)
            {
                PopulateLoadLayoutButtons();
            }
        }
    }

    private void PopulateLoadLayoutButtons()
    {
        if (LoadLayoutButtonContainer == null || LoadLayoutButtonPrefab == null)
            return;

        foreach (Transform child in LoadLayoutButtonContainer)
        {
            Destroy(child.gameObject);
        }

        List<string> layouts = GameStateManager.Instance?.GetSavedLayouts();
        if (layouts == null || layouts.Count == 0)
        {
            Debug.Log("[GameUIManager] No saved layouts found");
            return;
        }

        foreach (string filePath in layouts)
        {
            GameObject buttonObj = Instantiate(LoadLayoutButtonPrefab, LoadLayoutButtonContainer);
            
            Button[] buttons = buttonObj.GetComponentsInChildren<Button>();
            TextMeshProUGUI label = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            if (label != null)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                label.text = fileName;
            }

            if (buttons.Length >= 1)
            {
                Button loadButton = buttons[0];
                string path = filePath;
                loadButton.onClick.AddListener(() =>
                {
                    GameStateManager.Instance?.LoadLayout(path);
                    LoadLayoutPanel.SetActive(false);
                });
            }

            if (buttons.Length >= 2)
            {
                Button deleteButton = buttons[1];
                string path = filePath;
                string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                deleteButton.onClick.AddListener(() => OnDeleteLayoutClicked(path, fileName));
            }
        }
    }

    private void OnDeleteLayoutClicked(string filePath, string fileName)
    {
        _fileToDelete = filePath;
        
        if (ConfirmDeletePanel != null)
        {
            ConfirmDeletePanel.SetActive(true);
            
            if (ConfirmDeleteText != null)
            {
                ConfirmDeleteText.text = $"Delete '{fileName}'?\nThis action cannot be undone.";
            }
        }
    }

    private void OnConfirmDeleteYes()
    {
        if (!string.IsNullOrEmpty(_fileToDelete) && System.IO.File.Exists(_fileToDelete))
        {
            System.IO.File.Delete(_fileToDelete);
            Debug.Log($"[GameUIManager] Deleted layout: {_fileToDelete}");
            
            PopulateLoadLayoutButtons();
        }

        _fileToDelete = null;
        if (ConfirmDeletePanel != null)
        {
            ConfirmDeletePanel.SetActive(false);
        }
    }

    private void OnConfirmDeleteNo()
    {
        _fileToDelete = null;
        if (ConfirmDeletePanel != null)
        {
            ConfirmDeletePanel.SetActive(false);
        }
    }

    public bool ArePanelsActive()
    {
        return (SaveLayoutPanel != null && SaveLayoutPanel.activeSelf) ||
               (LoadLayoutPanel != null && LoadLayoutPanel.activeSelf) ||
               (PauseMenuPanel != null && PauseMenuPanel.activeSelf);
    }

    void Update()
    {
        if (GameStateManager.Instance == null) return;

        // ESC key - Show/hide pause menu at ANY time (not just simulate/win)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // If pause menu is showing, close it
            if (IsPauseMenuActive())
            {
                HidePauseMenu();
                return;
            }
            
            // Close prep panels first (if any are open)
            if (SaveLayoutPanel != null && SaveLayoutPanel.activeSelf)
            {
                SaveLayoutPanel.SetActive(false);
                return;
            }
            if (LoadLayoutPanel != null && LoadLayoutPanel.activeSelf)
            {
                LoadLayoutPanel.SetActive(false);
                return;
            }
            if (ConfirmDeletePanel != null && ConfirmDeletePanel.activeSelf)
            {
                ConfirmDeletePanel.SetActive(false);
                return;
            }
            
            // Show pause menu (works in any state)
            ShowPauseMenu();
            return;
        }

        // Don't process other shortcuts if panels are active
        if (ArePanelsActive()) return;

        // SPACEBAR - Pause/Resume during simulation OR start simulation in prep
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (GameStateManager.Instance.CurrentState == GameState.Simulate)
            {
                GameStateManager.Instance.TogglePause();
            }
            else if (GameStateManager.Instance.CurrentState == GameState.Prep)
            {
                GameStateManager.Instance.StartSimulation();
            }
        }

        // R - Return to Prep during simulation
        if (Input.GetKeyDown(KeyCode.R) && GameStateManager.Instance.CurrentState == GameState.Simulate)
        {
            GameStateManager.Instance.ReturnToPrep();
        }

        // Unit selection shortcuts (1-3) - Prep only
        if (GameStateManager.Instance.CurrentState == GameState.Prep)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) && GameStateManager.Instance.AvailableUnits.Count > 0)
            {
                GameStateManager.Instance.SelectUnit(GameStateManager.Instance.AvailableUnits[0]);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2) && GameStateManager.Instance.AvailableUnits.Count > 1)
            {
                GameStateManager.Instance.SelectUnit(GameStateManager.Instance.AvailableUnits[1]);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3) && GameStateManager.Instance.AvailableUnits.Count > 2)
            {
                GameStateManager.Instance.SelectUnit(GameStateManager.Instance.AvailableUnits[2]);
            }

            // S - Save layout
            if (Input.GetKeyDown(KeyCode.S))
            {
                OnSaveLayoutClicked();
            }

            // L - Load layout
            if (Input.GetKeyDown(KeyCode.L))
            {
                OnLoadLayoutClicked();
            }

            // C - Clear all teams
            if (Input.GetKeyDown(KeyCode.C))
            {
                OnClearAllClicked();
            }
        }
    }
}