using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Faller/Roller pre-click trajectory preview using a pooled dot line (Angry Birds style).
/// </summary>
public sealed class SpawnTrajectoryPreview : MonoBehaviour
{
    private const float GameplayPlaneZ = 0f;

    [SerializeField] private PlayerObstacleInput _obstacleInput;
    [SerializeField] private PlayerObstacleSpawner _spawner;
    [SerializeField] private Camera _camera;
    [SerializeField] private GameObject _dotPrefab;
    [SerializeField] private Transform _dotRoot;

    [Header("Path")]
    [SerializeField, Min(0.05f)] private float _dotSpacing = 0.3f;
    [SerializeField, Min(0f)] private float _viewportBottomMargin = 0.5f;
    [SerializeField, Min(1)] private int _initialPoolSize = 48;

    [Header("Dot Appearance")]
    [SerializeField, Min(0.01f)] private float _dotScale = 0.75f;
    [SerializeField, Range(0f, 1f)] private float _baseAlpha = 0.8f;
    [SerializeField, Range(0f, 1f)] private float _minAlpha = 0.3f;
    [SerializeField, Min(0.1f)] private float _fadeDistance = 10f;
    [SerializeField] private int _sortingOrder = 100;
    [SerializeField] private string _sortingLayerName = "Default";

    private readonly List<GameObject> _dotPool = new();
    private int _activeDotCount;
    private ObstacleKind _previewKind = (ObstacleKind)(-1);
    private Sprite _runtimeDotSprite;

    private void Awake()
    {
        if (_camera == null)
            _camera = Camera.main;

        if (_obstacleInput == null)
            _obstacleInput = FindFirstObjectByType<PlayerObstacleInput>();

        if (_spawner == null)
            _spawner = FindFirstObjectByType<PlayerObstacleSpawner>();

        if (_dotRoot == null)
        {
            var rootObject = new GameObject("TrajectoryDotRoot");
            _dotRoot = rootObject.transform;
            _dotRoot.SetParent(transform);
        }

        EnsureDotPrefab();
        PrewarmPool(_initialPoolSize);
    }

    private void OnEnable()
    {
        if (_obstacleInput == null)
            return;

        _obstacleInput.ObstacleKindChanged += OnObstacleKindChanged;
        OnObstacleKindChanged(_obstacleInput.CurrentObstacleKind);
    }

    private void OnDisable()
    {
        if (_obstacleInput != null)
            _obstacleInput.ObstacleKindChanged -= OnObstacleKindChanged;

        HideAllDots();
    }

    private void LateUpdate()
    {
        if (_previewKind != ObstacleKind.Faller && _previewKind != ObstacleKind.Roller)
            return;

        if (_obstacleInput == null || _camera == null)
        {
            HideAllDots();
            return;
        }

        if (!_obstacleInput.TryGetSpawnPositionFromScreen(Input.mousePosition, out Vector2 spawnPosition))
        {
            HideAllDots();
            return;
        }

        float endY = ResolvePathEndY(spawnPosition, _previewKind);
        if (endY >= spawnPosition.y)
        {
            HideAllDots();
            return;
        }

        PlaceDotsAlongVertical(spawnPosition, endY);
    }

    private void OnObstacleKindChanged(ObstacleKind kind)
    {
        if (kind == ObstacleKind.Faller || kind == ObstacleKind.Roller)
        {
            _previewKind = kind;
            return;
        }

        _previewKind = (ObstacleKind)(-1);
        HideAllDots();
    }

    private float ResolvePathEndY(Vector2 spawnPosition, ObstacleKind kind)
    {
        float maxEndY = GetViewportBottomWorldY();

        if (kind == ObstacleKind.Roller)
            return ResolveRollerEndY(spawnPosition, maxEndY);

        return maxEndY;
    }

    private float ResolveRollerEndY(Vector2 spawnPosition, float maxEndY)
    {
        ObstacleTuningConfig tuning = _spawner != null ? _spawner.Tuning : null;
        if (tuning == null)
            return maxEndY;

        float radius = tuning.Roller.GroundCheckRadius;
        float maxDistance = spawnPosition.y - maxEndY;
        if (maxDistance <= 0f)
            return maxEndY;

        var hit = Physics2D.CircleCast(
            spawnPosition,
            radius,
            Vector2.down,
            maxDistance,
            tuning.Roller.PlatformLayers);

        if (hit.collider == null || hit.collider.isTrigger)
            return maxEndY;

        return hit.point.y + radius;
    }

    private float GetViewportBottomWorldY()
    {
        float zDistance = Mathf.Abs(_camera.transform.position.z - GameplayPlaneZ);
        Vector3 bottomWorld = _camera.ViewportToWorldPoint(new Vector3(0.5f, 0f, zDistance));
        return bottomWorld.y - _viewportBottomMargin;
    }

    private void PlaceDotsAlongVertical(Vector2 spawnPosition, float endY)
    {
        _activeDotCount = 0;
        Vector2 cursor = spawnPosition;
        float step = Mathf.Max(_dotSpacing, 0.05f);

        while (cursor.y > endY && _activeDotCount < _dotPool.Count)
        {
            ActivateDot(_activeDotCount, cursor, spawnPosition);
            _activeDotCount++;
            cursor.y -= step;
        }

        while (cursor.y > endY)
        {
            ExpandPool();
            ActivateDot(_activeDotCount, cursor, spawnPosition);
            _activeDotCount++;
            cursor.y -= step;
        }

        for (int i = _activeDotCount; i < _dotPool.Count; i++)
        {
            if (_dotPool[i] != null)
                _dotPool[i].SetActive(false);
        }
    }

    private void ActivateDot(int index, Vector2 worldPosition, Vector2 spawnPosition)
    {
        GameObject dot = _dotPool[index];
        if (dot == null)
            return;

        dot.SetActive(true);
        dot.transform.position = new Vector3(worldPosition.x, worldPosition.y, GameplayPlaneZ);
        dot.transform.localScale = Vector3.one * _dotScale;

        if (!dot.TryGetComponent(out SpriteRenderer spriteRenderer))
            return;

        float distance = Vector2.Distance(spawnPosition, worldPosition);
        float alpha = Mathf.Lerp(_baseAlpha, _minAlpha, distance / _fadeDistance);
        alpha = Mathf.Clamp(alpha, _minAlpha, _baseAlpha);

        Color color = spriteRenderer.color;
        color.a = alpha;
        spriteRenderer.color = color;
    }

    private void HideAllDots()
    {
        _activeDotCount = 0;
        for (int i = 0; i < _dotPool.Count; i++)
        {
            if (_dotPool[i] != null)
                _dotPool[i].SetActive(false);
        }
    }

    private void PrewarmPool(int count)
    {
        for (int i = _dotPool.Count; i < count; i++)
            ExpandPool();
    }

    private void ExpandPool()
    {
        GameObject dot = _dotPrefab != null
            ? Instantiate(_dotPrefab, _dotRoot)
            : CreateRuntimeDot();

        dot.SetActive(false);
        _dotPool.Add(dot);
    }

    private GameObject CreateRuntimeDot()
    {
        var dot = new GameObject("TrajectoryDot");
        dot.transform.SetParent(_dotRoot, false);

        var spriteRenderer = dot.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetRuntimeDotSprite();
        spriteRenderer.color = new Color(1f, 1f, 1f, _baseAlpha);
        spriteRenderer.sortingOrder = _sortingOrder;
        if (!string.IsNullOrEmpty(_sortingLayerName))
            spriteRenderer.sortingLayerName = _sortingLayerName;

        return dot;
    }

    private void EnsureDotPrefab()
    {
        if (_dotPrefab != null)
            return;

        _runtimeDotSprite = CreateCircleSprite();
    }

    private Sprite GetRuntimeDotSprite()
    {
        if (_runtimeDotSprite == null)
            _runtimeDotSprite = CreateCircleSprite();

        return _runtimeDotSprite;
    }

    private static Sprite CreateCircleSprite()
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var center = new Vector2(size * 0.5f, size * 0.5f);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance < 14f ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
