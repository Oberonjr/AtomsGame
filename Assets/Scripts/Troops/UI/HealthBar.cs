using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("References")]
    public Slider HealthSlider;
    public Image FillImage;

    [Header("Colors")]
    public Color FullHealthColor = Color.green;
    public Color LowHealthColor = Color.red;

    [Header("Settings")]
    public Vector3 Offset = new Vector3(0, 1f, 0);
    public bool FaceCamera = true;

    private Troop _troop;
    private Camera _mainCamera;

    void Start()
    {
        _troop = GetComponentInParent<Troop>();
        _mainCamera = Camera.main;

        if (_troop != null && HealthSlider != null)
        {
            HealthSlider.maxValue = _troop.TroopStats.MaxHealth;
            HealthSlider.value = _troop.CurrentHealth;
        }
    }

    void Update()
    {
        if (_troop != null && HealthSlider != null)
        {
            HealthSlider.value = _troop.CurrentHealth;

            // Update color based on health percentage
            float healthPercent = (float)_troop.CurrentHealth / _troop.TroopStats.MaxHealth;
            if (FillImage != null)
            {
                FillImage.color = Color.Lerp(LowHealthColor, FullHealthColor, healthPercent);
            }
        }

        // Position above troop
        if (_troop != null)
        {
            transform.position = _troop.transform.position + Offset;
        }

        // Face camera
        if (FaceCamera && _mainCamera != null)
        {
            transform.rotation = _mainCamera.transform.rotation;
        }
    }
}