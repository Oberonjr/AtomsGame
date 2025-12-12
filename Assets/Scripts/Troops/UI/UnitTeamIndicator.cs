using UnityEngine;

/// <summary>
/// Shows a colored circle under units during simulation to indicate team
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class UnitTeamIndicator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _radius = 1.2f;
    [SerializeField] private int _sortingOrder = -1;
    [SerializeField][Range(0, 255)] private int _alpha = 120;

    private SpriteRenderer _spriteRenderer;
    private ITroop _troop;
    private bool _isInitialized = false;
    private Texture2D _generatedTexture; // Store reference

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // Create circle sprite
        _spriteRenderer.sprite = CreateCircleSprite();
        _spriteRenderer.sortingOrder = _sortingOrder;

        // Set scale based on radius
        transform.localScale = Vector3.one * _radius;

        // Start hidden
        _spriteRenderer.enabled = false;
    }

    void Start()
    {
        _troop = GetComponentInParent<ITroop>();
        
        if (_troop == null)
        {
            Debug.LogError($"[UnitTeamIndicator] No ITroop found on {name}!");
            enabled = false; // Disable the component
            return;
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
            OnGameStateChanged(GameStateManager.Instance.CurrentState);
        }

        _isInitialized = true;
        UpdateTeamColor();
    }

    void OnDestroy()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
        }
        
        // Clean up generated texture
        if (_generatedTexture != null)
        {
            Destroy(_generatedTexture);
        }
    }

    void LateUpdate()
    {
        // Keep at ground level (z = 0)
        Vector3 pos = transform.localPosition;
        pos.z = 0f;
        transform.localPosition = pos;
    }

    private void OnGameStateChanged(GameState newState)
    {
        // Only show during simulation
        bool shouldShow = newState == GameState.Simulate;
        _spriteRenderer.enabled = shouldShow;

        if (shouldShow)
        {
            UpdateTeamColor();
        }
    }

    private void UpdateTeamColor()
    {
        if (_troop == null || _spriteRenderer == null) return;

        // Get team color from TeamManager
        Team team = GetTeamForTroop();

        if (team != null)
        {
            Color teamColor = team.TeamColor;
            teamColor.a = _alpha / 255f; // Convert to 0-1 range
            _spriteRenderer.color = teamColor;
        }
        else
        {
            Debug.LogWarning($"[UnitTeamIndicator] Could not find team for {_troop.GameObject?.name}");
        }
    }

    private Team GetTeamForTroop()
    {
        if (TeamManager.Instance == null) return null;

        int teamIndex = _troop.TeamIndex;
        return TeamManager.Instance.GetTeamByIndex(teamIndex);
    }

    private Sprite CreateCircleSprite()
    {
        int resolution = 128;
        _generatedTexture = new Texture2D(resolution, resolution);
        Color[] colors = new Color[resolution * resolution];

        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float radius = resolution / 2f;

        // Create smooth circle
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);

                if (distance <= radius)
                {
                    // Solid white circle (color will be tinted by SpriteRenderer)
                    colors[y * resolution + x] = Color.white;
                }
                else
                {
                    colors[y * resolution + x] = Color.clear;
                }
            }
        }

        _generatedTexture.SetPixels(colors);
        _generatedTexture.Apply();
        _generatedTexture.filterMode = FilterMode.Bilinear;

        return Sprite.Create(
            _generatedTexture,
            new Rect(0, 0, resolution, resolution),
            new Vector2(0.5f, 0.5f),
            resolution / (2f * _radius) // Pixels per unit
        );
    }
}