using UnityEngine;
using UnityEngine.UI;
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
    public Button PauseButton;
    public Button BackToPrepButton;
    public TextMeshProUGUI PauseButtonText;

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
    public GameObject LoadLayoutPanel;
    public Transform LoadLayoutButtonContainer;
    public GameObject LoadLayoutButtonPrefab;

    private List<Button> _unitButtons = new List<Button>();
    private List<Button> _clearTeamButtons = new List<Button>();

    void Start()
    {
        Debug.Log("[GameUIManager] Initializing...");
        
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged += OnStateChanged;
            GameStateManager.Instance.OnUnitSelected += OnUnitSelected;
            GameStateManager.Instance.OnTeamWon += OnTeamWon;
            GameStateManager.Instance.OnPauseChanged += OnPauseChanged;

            CreateUnitButtons();
            CreateClearTeamButtons(); // ADD THIS LINE
            SetupButtons();
            OnStateChanged(GameStateManager.Instance.CurrentState);
            
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
            GameStateManager.Instance.OnPauseChanged -= OnPauseChanged;
        }
    }

    private void CreateUnitButtons()
    {
        if (GameStateManager.Instance == null || UnitButtonContainer == null || UnitButtonPrefab == null)
        {
            Debug.LogError($"[GameUIManager] Cannot create unit buttons - missing references. GameStateManager: {GameStateManager.Instance != null}, UnitButtonContainer: {UnitButtonContainer != null}, UnitButtonPrefab: {UnitButtonPrefab != null}");
            return;
        }

        Debug.Log($"[GameUIManager] Creating {GameStateManager.Instance.AvailableUnits.Count} unit buttons");

        foreach (var unitData in GameStateManager.Instance.AvailableUnits)
        {
            if (unitData == null)
            {
                Debug.LogWarning("[GameUIManager] Skipping null unitData");
                continue;
            }
            
            GameObject buttonObj = Instantiate(UnitButtonPrefab, UnitButtonContainer);
            Button button = buttonObj.GetComponent<Button>();

            if (button != null)
            {
                // Setup button visuals
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = unitData.DisplayName;
                    Debug.Log($"[GameUIManager] Set button text to: {unitData.DisplayName}");
                }
                else
                {
                    Debug.LogWarning($"[GameUIManager] No TextMeshProUGUI found in button children for: {unitData.DisplayName}");
                }

                // Try to find icon image on the child of the button
                Image buttonImage = buttonObj.transform.GetChild(0).GetComponent<Image>();
                if (buttonImage == null)
                {
                    buttonImage = buttonObj.GetComponentInChildren<Image>();
                }
                
                if (buttonImage != null && unitData.Icon != null)
                {
                    buttonImage.sprite = unitData.Icon;
                    Debug.Log($"[GameUIManager] Set button icon for: {unitData.DisplayName}");
                }

                // Setup button click
                UnitSelectionData capturedData = unitData;
                button.onClick.AddListener(() =>
                {
                    Debug.Log($"[GameUIManager] Button clicked for: {capturedData.DisplayName}");
                    GameStateManager.Instance.SelectUnit(capturedData);
                });

                _unitButtons.Add(button);
            }
            else
            {
                Debug.LogError($"[GameUIManager] UnitButtonPrefab does not have a Button component!");
            }
        }
        
        Debug.Log($"[GameUIManager] Created {_unitButtons.Count} unit buttons");
    }

    private void CreateClearTeamButtons()
    {
        if (TeamManager.Instance == null || ClearTeamButtonContainer == null || ClearTeamButtonPrefab == null)
        {
            Debug.LogWarning("[GameUIManager] Cannot create clear team buttons - missing references");
            return;
        }

        foreach (Team team in TeamManager.Instance.Teams)
        {
            if (team == null) continue;

            GameObject buttonObj = Instantiate(ClearTeamButtonPrefab, ClearTeamButtonContainer);
            Button button = buttonObj.GetComponent<Button>();

            if (button != null)
            {
                // Setup button visuals
                TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = $"Clear {team.TeamColor}";
                }

                Image buttonImage = button.GetComponent<Image>();
                if (buttonImage != null)
                {
                    Color color = team.TeamColor;
                    color.a = 0.5f;
                    buttonImage.color = color;
                }

                // Add click listener
                System.Guid teamID = team.ID; // Capture in closure
                button.onClick.AddListener(() => OnClearTeamClicked(teamID));

                _clearTeamButtons.Add(button);
            }
        }

        Debug.Log($"[GameUIManager] Created {_clearTeamButtons.Count} clear team buttons");
    }

    private void SetupButtons()
    {
        Debug.Log("[GameUIManager] Setting up control buttons...");
        
        if (SimulateButton != null)
            SimulateButton.onClick.AddListener(() => GameStateManager.Instance.StartSimulation());
        else
            Debug.LogWarning("[GameUIManager] SimulateButton is null!");

        if (PauseButton != null)
            PauseButton.onClick.AddListener(() => GameStateManager.Instance.TogglePause());
        else
            Debug.LogWarning("[GameUIManager] PauseButton is null!");

        if (BackToPrepButton != null)
            BackToPrepButton.onClick.AddListener(() => GameStateManager.Instance.ReturnToPrep());
        else
            Debug.LogWarning("[GameUIManager] BackToPrepButton is null!");

        if (RestartButton != null)
            RestartButton.onClick.AddListener(() => GameStateManager.Instance.RestartSimulation());
        else
            Debug.LogWarning("[GameUIManager] RestartButton is null!");

        if (BackToPrepFromWinButton != null)
            BackToPrepFromWinButton.onClick.AddListener(() => GameStateManager.Instance.ReturnToPrep());
        else
            Debug.LogWarning("[GameUIManager] BackToPrepFromWinButton is null!");

        // Team management buttons
        if (ClearAllButton != null)
            ClearAllButton.onClick.AddListener(OnClearAllClicked);
        else
            Debug.LogWarning("[GameUIManager] ClearAllButton is null!");

        // Save/load buttons
        if (SaveLayoutButton != null)
            SaveLayoutButton.onClick.AddListener(OnSaveLayoutClicked);
        else
            Debug.LogWarning("[GameUIManager] SaveLayoutButton is null!");

        if (LoadLayoutButton != null)
            LoadLayoutButton.onClick.AddListener(OnLoadLayoutClicked);
        else
            Debug.LogWarning("[GameUIManager] LoadLayoutButton is null!");
    }

    private void OnStateChanged(GameState newState)
    {
        Debug.Log($"[GameUIManager] State changed to: {newState}");
        
        if (PrepUI != null) PrepUI.SetActive(newState == GameState.Prep);
        if (SimulateUI != null) SimulateUI.SetActive(newState == GameState.Simulate);
        if (WinUI != null) WinUI.SetActive(newState == GameState.Win);

        if (LoadLayoutPanel != null && newState != GameState.Prep)
        {
            LoadLayoutPanel.SetActive(false);
        }
    }

    private void OnUnitSelected(UnitSelectionData unit)
    {
        Debug.Log($"[GameUIManager] OnUnitSelected called. Unit: {(unit != null ? unit.DisplayName : "None")}");
        
        // Visual feedback for selected unit
        foreach (Button button in _unitButtons)
        {
            if (button != null)
            {
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                button.colors = colors;
            }
        }

        if (unit != null)
        {
            int index = GameStateManager.Instance.AvailableUnits.IndexOf(unit);
            Debug.Log($"[GameUIManager] Selected unit index: {index}");
            
            if (index >= 0 && index < _unitButtons.Count)
            {
                ColorBlock colors = _unitButtons[index].colors;
                colors.normalColor = Color.yellow;
                _unitButtons[index].colors = colors;
                Debug.Log($"[GameUIManager] Highlighted button at index: {index}");
            }
            else
            {
                Debug.LogWarning($"[GameUIManager] Index out of range: {index} (button count: {_unitButtons.Count})");
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
                // Draw
                WinnerText.text = "Draw!";
                WinnerText.color = Color.white;
                if (WinnerColorDisplay != null)
                {
                    WinnerColorDisplay.color = Color.gray;
                }
            }
            else
            {
                // Team won
                WinnerText.text = $"{winner.TeamColor} Team Wins!";
                WinnerText.color = winner.TeamColor;
                if (WinnerColorDisplay != null)
                {
                    WinnerColorDisplay.color = winner.TeamColor;
                }
            }
        }
    }

    private void OnPauseChanged(bool isPaused)
    {
        Debug.Log($"[GameUIManager] Pause state changed: {isPaused}");
        
        if (PauseButtonText != null)
        {
            PauseButtonText.text = isPaused ? "Resume" : "Pause";
        }
    }

    private void OnClearTeamClicked(System.Guid teamID)
    {
        Debug.Log($"[GameUIManager] Clear team button clicked: {teamID}");
        GameStateManager.Instance?.ClearTeam(teamID);
    }

    private void OnClearAllClicked()
    {
        Debug.Log("[GameUIManager] Clear all button clicked");
        GameStateManager.Instance?.ClearAllTeams();
    }

    private void OnSaveLayoutClicked()
    {
        string layoutName = SaveNameInput != null && !string.IsNullOrEmpty(SaveNameInput.text)
            ? SaveNameInput.text
            : "Layout";

        Debug.Log($"[GameUIManager] Save layout button clicked: {layoutName}");
        GameStateManager.Instance?.SaveCurrentLayout(layoutName);

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
            Button button = buttonObj.GetComponent<Button>();

            if (button != null)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                
                TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = fileName;
                }

                string path = filePath;
                button.onClick.AddListener(() =>
                {
                    GameStateManager.Instance?.LoadLayout(path);
                    LoadLayoutPanel.SetActive(false);
                });
            }
        }
    }
}