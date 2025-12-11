using UnityEngine;
using UnityEngine.UI;
using UnityAtoms.BaseAtoms;

/// <summary>
/// Atoms-specific health bar that subscribes to Atoms Variable changes for reactive updates
/// Demonstrates full Atoms reactivity with UI elements
/// </summary>
public class HealthBar_Atoms : MonoBehaviour, IHealthBar
{
    [Header("UI References")]
    public Slider HealthSlider;
    public Image FillImage;

    [Header("Colors")]
    public Color FullHealthColor = Color.green;
    public Color LowHealthColor = Color.red;

    [Header("Settings")]
    public Vector3 Offset = new Vector3(0, 1f, 0);
    public bool FaceCamera = true;

    [Header("Atoms Variables (Auto-detected)")]
    [Tooltip("Automatically found from parent Troop_Atoms")]
    private IntVariableInstancer _healthInstancer;
    private IntVariable _maxHealthVariable;

    private Troop_Atoms _troopAtoms;
    private Camera _mainCamera;
    private int _cachedMaxHealth;

    void Start()
    {
        _mainCamera = Camera.main;

        // Find parent Troop_Atoms
        _troopAtoms = GetComponentInParent<Troop_Atoms>();

        if (_troopAtoms != null)
        {
            Initialize(_troopAtoms);
        }
        else
        {
            Debug.LogError("[HealthBar_Atoms] No Troop_Atoms found in parent! This healthbar only works with Atoms troops.");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from Atoms health changes
        UnsubscribeFromHealthChanges();
    }

    public void Initialize(ITroop troop)
    {
        _troopAtoms = troop as Troop_Atoms;

        if (_troopAtoms == null)
        {
            Debug.LogError("[HealthBar_Atoms] ITroop is not a Troop_Atoms! Cannot initialize.");
            return;
        }

        // Get the health instancer from Troop_Atoms
        _healthInstancer = _troopAtoms.GetComponent<IntVariableInstancer>();

        if (_healthInstancer == null || _healthInstancer.Variable == null)
        {
            Debug.LogError($"[HealthBar_Atoms] No IntVariableInstancer found on {_troopAtoms.name}!");
            return;
        }

        // Get max health from Atoms stats
        _cachedMaxHealth = AtomsVariableConverter.ToInt(_troopAtoms.Stats.MaxHealth, 100);

        // Setup slider
        if (HealthSlider != null)
        {
            HealthSlider.maxValue = _cachedMaxHealth;
            HealthSlider.value = _healthInstancer.Variable.Value;
        }

        // Subscribe to reactive health changes
        SubscribeToHealthChanges();

        // Initial display update
        UpdateDisplay(_healthInstancer.Variable.Value, _cachedMaxHealth);

        Debug.Log($"[HealthBar_Atoms] Initialized for {_troopAtoms.name} - Max Health: {_cachedMaxHealth}, Current: {_healthInstancer.Variable.Value}");
    }

    public void UpdateDisplay(int currentHealth, int maxHealth)
    {
        if (HealthSlider != null)
        {
            HealthSlider.value = currentHealth;
        }

        // Update color based on health percentage
        if (FillImage != null && maxHealth > 0)
        {
            float healthPercent = (float)currentHealth / maxHealth;
            FillImage.color = Color.Lerp(LowHealthColor, FullHealthColor, healthPercent);
        }
    }

    void Update()
    {
        if (_troopAtoms == null) return;

        // Position above troop
        if (_troopAtoms.transform != null)
        {
            transform.position = _troopAtoms.transform.position + Offset;
        }

        // Face camera
        if (FaceCamera && _mainCamera != null)
        {
            transform.rotation = _mainCamera.transform.rotation;
        }
    }

    // ========== ATOMS SUBSCRIPTION ==========

    private void SubscribeToHealthChanges()
    {
        if (_healthInstancer == null || _healthInstancer.Variable == null) return;

        _healthInstancer.Variable.Changed.Register(OnHealthChanged);
        Debug.Log($"[HealthBar_Atoms] Subscribed to health changes for {_troopAtoms.name}");
    }

    private void UnsubscribeFromHealthChanges()
    {
        if (_healthInstancer == null || _healthInstancer.Variable == null) return;

        _healthInstancer.Variable.Changed.Unregister(OnHealthChanged);
        Debug.Log($"[HealthBar_Atoms] Unsubscribed from health changes");
    }

    /// <summary>
    /// Called reactively when Atoms health variable changes
    /// </summary>
    private void OnHealthChanged(int newHealth)
    {
        UpdateDisplay(newHealth, _cachedMaxHealth);

        if (SimulationConfig.Instance != null && SimulationConfig.Instance.VerboseLogging)
        {
            Debug.Log($"[HealthBar_Atoms] Health changed to {newHealth}/{_cachedMaxHealth}");
        }
    }
}