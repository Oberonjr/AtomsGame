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

    private List<Button> _unitButtons = new List<Button>();

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
    }

    private void OnStateChanged(GameState newState)
    {
        Debug.Log($"[GameUIManager] State changed to: {newState}");
        
        if (PrepUI != null) PrepUI.SetActive(newState == GameState.Prep);
        if (SimulateUI != null) SimulateUI.SetActive(newState == GameState.Simulate);
        if (WinUI != null) WinUI.SetActive(newState == GameState.Win);
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
        Debug.Log($"[GameUIManager] Team won!");
        
        if (WinnerText != null)
        {
            WinnerText.text = $"Team Wins!";
            WinnerText.color = winner.TeamColor;
        }

        if (WinnerColorDisplay != null)
        {
            WinnerColorDisplay.color = winner.TeamColor;
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
}