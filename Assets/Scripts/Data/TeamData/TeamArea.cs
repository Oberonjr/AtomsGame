using UnityEngine;

public class TeamArea : MonoBehaviour
{
    public float Radius = 5f;
    [SerializeField] private Color _teamColor = Color.white;
    public bool ShowGizmo = true;
    public bool ShowInnerArea = true;
    public bool ShowBorder = true;
    public float BorderThickness = 0.1f;

    [Header("Runtime Visualization")]
    public GameObject CircleSpritePrefab; // Optional: Assign a circle sprite prefab
    public bool ShowRuntimeCircle = true;

    private Team _subscribedTeam;
    private GameObject _runtimeCircle;
    private SpriteRenderer _circleRenderer;

    public Color TeamColor => _teamColor;

    void Start()
    {
        // Subscribe to game state changes
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
            
            // Initialize based on current state
            OnGameStateChanged(GameStateManager.Instance.CurrentState);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from team
        if (_subscribedTeam != null)
        {
            _subscribedTeam.OnTeamColorChanged -= OnTeamColorChanged;
        }

        // Unsubscribe from game state
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
        }

        // Clean up runtime circle
        if (_runtimeCircle != null)
        {
            Destroy(_runtimeCircle);
        }
    }

    public void SubscribeToTeam(Team team)
    {
        if (_subscribedTeam != null)
        {
            _subscribedTeam.OnTeamColorChanged -= OnTeamColorChanged;
        }

        _subscribedTeam = team;

        if (_subscribedTeam != null)
        {
            _subscribedTeam.OnTeamColorChanged += OnTeamColorChanged;
        }
    }

    public void UnsubscribeFromTeam(Team team)
    {
        if (_subscribedTeam == team)
        {
            _subscribedTeam.OnTeamColorChanged -= OnTeamColorChanged;
            _subscribedTeam = null;
        }
    }

    public void SetTeamColor(Color color)
    {
        _teamColor = color;
        
        // Update runtime circle if it exists
        if (_circleRenderer != null)
        {
            Color circleColor = _teamColor;
            circleColor.a = 0.3f;
            _circleRenderer.color = circleColor;
        }
    }

    private void OnTeamColorChanged(Color newColor)
    {
        SetTeamColor(newColor);
    }

    private void OnGameStateChanged(GameState newState)
    {
        if (!ShowRuntimeCircle) return;

        if (newState == GameState.Prep)
        {
            ShowVisualCircle();
        }
        else
        {
            HideVisualCircle();
        }
    }

    private void ShowVisualCircle()
    {
        if (_runtimeCircle == null)
        {
            CreateRuntimeCircle();
        }

        if (_runtimeCircle != null)
        {
            _runtimeCircle.SetActive(true);
        }
    }

    private void HideVisualCircle()
    {
        if (_runtimeCircle != null)
        {
            _runtimeCircle.SetActive(false);
        }
    }

    private void CreateRuntimeCircle()
    {
        // Use prefab if provided
        if (CircleSpritePrefab != null)
        {
            _runtimeCircle = Instantiate(CircleSpritePrefab, transform.position, Quaternion.identity, transform);
            _runtimeCircle.transform.localPosition = Vector3.zero;
            _circleRenderer = _runtimeCircle.GetComponent<SpriteRenderer>();
        }
        else
        {
            // Create circle GameObject
            _runtimeCircle = new GameObject("RuntimeAreaCircle");
            _runtimeCircle.transform.SetParent(transform);
            _runtimeCircle.transform.localPosition = Vector3.zero;
            
            // Add SpriteRenderer
            _circleRenderer = _runtimeCircle.AddComponent<SpriteRenderer>();
            
            // Create circle sprite
            _circleRenderer.sprite = CreateCircleSprite();
            
            // Set sorting order to render behind units
            _circleRenderer.sortingOrder = -10;
        }

        if (_circleRenderer != null)
        {
            // Set color with transparency
            Color circleColor = _teamColor;
            circleColor.a = 0.3f;
            _circleRenderer.color = circleColor;
            
            // Scale to match radius (assuming sprite is 1 unit = 1 world unit)
            float scale = Radius * 2f;
            _runtimeCircle.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    private Sprite CreateCircleSprite()
    {
        int resolution = 128;
        Texture2D texture = new Texture2D(resolution, resolution);
        Color[] colors = new Color[resolution * resolution];
        
        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float radius = resolution / 2f;

        // Create circle with soft edge
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                
                if (distance <= radius)
                {
                    // Solid interior
                    colors[y * resolution + x] = Color.white;
                }
                else
                {
                    // Transparent outside
                    colors[y * resolution + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        
        return Sprite.Create(
            texture, 
            new Rect(0, 0, resolution, resolution), 
            new Vector2(0.5f, 0.5f), 
            resolution / 2f // Pixels per unit
        );
    }

    private void OnDrawGizmos()
    {
        if (!ShowGizmo) return;

        // Cache the previous color
        Color oldColor = Gizmos.color;
        Matrix4x4 oldMatrix = Gizmos.matrix;

        // Apply the transform
        Gizmos.matrix = transform.localToWorldMatrix;

        // Draw inner area
        if (ShowInnerArea)
        {
            Gizmos.color = new Color(_teamColor.r, _teamColor.g, _teamColor.b, 0.2f);
            Gizmos.DrawSphere(Vector3.zero, Radius);
        }

        // Draw border
        if (ShowBorder)
        {
            Gizmos.color = _teamColor;
            Gizmos.DrawWireSphere(Vector3.zero, Radius);
        }

        // Restore previous color and matrix
        Gizmos.color = oldColor;
        Gizmos.matrix = oldMatrix;
    }
}