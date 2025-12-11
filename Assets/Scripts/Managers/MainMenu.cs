using UnityEngine;
using Udar.SceneManager;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [Header("Mode Selection Buttons")]
    [SerializeField] private Button _unityModeButton;
    [SerializeField] private Button _atomsModeButton;
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _quitButton;

    [Header("UI Display")]
    [SerializeField] private TextMeshProUGUI _selectedModeText;
    [SerializeField] private GameObject _unityModeIndicator;
    [SerializeField] private GameObject _atomsModeIndicator;

    [Header("Settings")]
    [SerializeField] private SceneField _gameScene;

    private SimulationMode _selectedMode = SimulationMode.Unity;

    void Start()
    {
        // Setup buttons
        if (_unityModeButton != null)
            _unityModeButton.onClick.AddListener(SelectUnityMode);

        if (_atomsModeButton != null)
            _atomsModeButton.onClick.AddListener(SelectAtomsMode);

        if (_startButton != null)
            _startButton.onClick.AddListener(StartGame);

        if (_quitButton != null)
            _quitButton.onClick.AddListener(QuitGame);

        // Initialize with Unity mode selected
        SelectUnityMode();
    }

    private void SelectUnityMode()
    {
        _selectedMode = SimulationMode.Unity;
        UpdateUI();
        Debug.Log("[MainMenu] Unity mode selected");
    }

    private void SelectAtomsMode()
    {
        _selectedMode = SimulationMode.Atoms;
        UpdateUI();
        Debug.Log("[MainMenu] Atoms mode selected");
    }

    private void UpdateUI()
    {
        // Update text
        if (_selectedModeText != null)
        {
            _selectedModeText.text = $"Selected Mode: {_selectedMode}";
        }

        // Update indicators
        if (_unityModeIndicator != null)
            _unityModeIndicator.SetActive(_selectedMode == SimulationMode.Unity);

        if (_atomsModeIndicator != null)
            _atomsModeIndicator.SetActive(_selectedMode == SimulationMode.Atoms);

        // Update button colors - FIXED: Force immediate visual update
        if (_unityModeButton != null)
        {
            Color targetColor = _selectedMode == SimulationMode.Unity ? Color.yellow : Color.white;
            
            // Update ColorBlock
            ColorBlock colors = _unityModeButton.colors;
            colors.normalColor = targetColor;
            colors.selectedColor = targetColor;
            _unityModeButton.colors = colors;
            
            // Force immediate visual update on button's Image component
            Image buttonImage = _unityModeButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = targetColor;
            }
        }

        if (_atomsModeButton != null)
        {
            Color targetColor = _selectedMode == SimulationMode.Atoms ? Color.yellow : Color.white;
            
            // Update ColorBlock
            ColorBlock colors = _atomsModeButton.colors;
            colors.normalColor = targetColor;
            colors.selectedColor = targetColor;
            _atomsModeButton.colors = colors;
            
            // Force immediate visual update on button's Image component
            Image buttonImage = _atomsModeButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = targetColor;
            }
        }
    }

    private void StartGame()
    {
        // Create or get SimulationConfig
        SimulationConfig config = FindObjectOfType<SimulationConfig>();

        if (config == null)
        {
            // Create new config GameObject
            GameObject configObj = new GameObject("SimulationConfig");
            config = configObj.AddComponent<SimulationConfig>();
        }

        // Set the mode
        config.SetMode(_selectedMode);

        Debug.Log($"[MainMenu] Starting game with {_selectedMode} mode");

        // Load game scene
        SceneManager.LoadScene(_gameScene.Name);
    }

    private void QuitGame()
    {
        Debug.Log("[MainMenu] Quitting game");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}