using UnityEngine;

/// <summary>
/// Villain input: Alpha1/2/3 select spawn mode; left click spawns Faller/Roller at viewport top X.
/// Bouncer mode uses click-drag-release; launch direction clamps to a minimum downward angle from horizontal.
/// </summary>
public sealed class PlayerObstacleInput : MonoBehaviour
{
    private const float MinDirectionSqrMagnitude = 0.0001f;
    private const float GameplayPlaneZ = 0f;
    private static readonly Color DefaultAimLineColor = new(0.35f, 0.9f, 1f, 0.9f);

    [SerializeField] private PlayerObstacleSpawner _spawner;
    [SerializeField] private MonoBehaviour _targetClimberBehaviour;
    [SerializeField] private Camera _camera;
    [SerializeField, Min(0f)] private float _bouncerMinDragScreenPixels = 8f;
    [SerializeField] private LineRenderer _bouncerAimLine;
    [SerializeField] private LineRenderer _bouncerAimHeadLeft;
    [SerializeField] private LineRenderer _bouncerAimHeadRight;
    [SerializeField, Min(0.01f)] private float _bouncerAimLineLength = 4f;
    [SerializeField, Min(0.01f)] private float _bouncerAimLineWidth = 0.15f;
    [SerializeField, Min(0.01f)] private float _bouncerAimHeadLength = 0.6f;
    [SerializeField, Range(5f, 85f)] private float _bouncerAimHeadAngleDegrees = 25f;
    [SerializeField] private string _bouncerAimLineSortingLayer = "Character";
    [SerializeField] private int _bouncerAimLineSortingOrder = 50;

    private Material _bouncerAimLineMaterial;
    private IClimberAgent _targetClimber;
    private ObstacleSpawnMode _mode = ObstacleSpawnMode.Faller;
    private bool _bouncerDragActive;
    private Vector2 _bouncerDragStartScreen;
    private float _bouncerAimWorldX;

    private void Awake()
    {
        if (_camera == null)
            _camera = Camera.main;

        if (_targetClimberBehaviour != null)
            _targetClimberBehaviour.TryGetComponent(out _targetClimber);

        EnsureBouncerAimHeadLines();
        ConfigureBouncerAimLines();
        SetBouncerAimLineVisible(false);
        EnsureValidMode();
    }

    private void EnsureBouncerAimHeadLines()
    {
        Transform parent = _bouncerAimLine != null ? _bouncerAimLine.transform : transform;

        if (_bouncerAimHeadLeft == null)
            _bouncerAimHeadLeft = CreateChildAimLine(parent, "BouncerAimHeadLeft");

        if (_bouncerAimHeadRight == null)
            _bouncerAimHeadRight = CreateChildAimLine(parent, "BouncerAimHeadRight");
    }

    private LineRenderer CreateChildAimLine(Transform parent, string objectName)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(parent, false);
        return go.AddComponent<LineRenderer>();
    }

    private void ConfigureBouncerAimLines()
    {
        if (_bouncerAimLineMaterial == null)
            _bouncerAimLineMaterial = CreateAimLineMaterial();

        ConfigureAimLineRenderer(_bouncerAimLine);
        ConfigureAimLineRenderer(_bouncerAimHeadLeft);
        ConfigureAimLineRenderer(_bouncerAimHeadRight);
    }

    private void ConfigureAimLineRenderer(LineRenderer line)
    {
        if (line == null)
            return;

        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = _bouncerAimLineWidth;
        line.endWidth = _bouncerAimLineWidth;
        line.startColor = DefaultAimLineColor;
        line.endColor = DefaultAimLineColor;
        line.alignment = LineAlignment.View;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sortingLayerName = _bouncerAimLineSortingLayer;
        line.sortingOrder = _bouncerAimLineSortingOrder;

        if (_bouncerAimLineMaterial != null)
            line.material = _bouncerAimLineMaterial;
    }

    private static Material CreateAimLineMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default")
            ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color");

        if (shader == null)
            return null;

        var material = new Material(shader);
        ApplyAimLineColor(material, DefaultAimLineColor);
        return material;
    }

    private static void ApplyAimLineColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }

    private void Update()
    {
        if (_spawner == null || _camera == null)
            return;

        HandleModeKeys();

        if (_mode != ObstacleSpawnMode.Bouncer && _bouncerDragActive)
            CancelBouncerDrag();

        if (_mode == ObstacleSpawnMode.Bouncer)
            HandleBouncerDrag();
        else
            HandleClickSpawn(_mode);
    }

    private void HandleModeKeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) && _spawner.IsKindAllowed(ObstacleKind.Faller))
            _mode = ObstacleSpawnMode.Faller;

        if (Input.GetKeyDown(KeyCode.Alpha2) &&
            _spawner.IsKindAllowed(ObstacleKind.Bouncer) &&
            _spawner.IsReady(ObstacleKind.Bouncer))
        {
            _mode = ObstacleSpawnMode.Bouncer;
        }

        if (Input.GetKeyDown(KeyCode.Alpha3) &&
            _spawner.IsKindAllowed(ObstacleKind.Roller) &&
            _spawner.IsReady(ObstacleKind.Roller))
        {
            _mode = ObstacleSpawnMode.Roller;
        }
    }

    private void EnsureValidMode()
    {
        if (_spawner == null)
            return;

        if (IsModeAllowed(_mode))
            return;

        if (_spawner.IsKindAllowed(ObstacleKind.Faller))
            _mode = ObstacleSpawnMode.Faller;
        else if (_spawner.IsKindAllowed(ObstacleKind.Bouncer))
            _mode = ObstacleSpawnMode.Bouncer;
        else if (_spawner.IsKindAllowed(ObstacleKind.Roller))
            _mode = ObstacleSpawnMode.Roller;
    }

    private bool IsModeAllowed(ObstacleSpawnMode mode)
    {
        if (_spawner == null)
            return false;

        return mode switch
        {
            ObstacleSpawnMode.Faller => _spawner.IsKindAllowed(ObstacleKind.Faller),
            ObstacleSpawnMode.Bouncer => _spawner.IsKindAllowed(ObstacleKind.Bouncer),
            ObstacleSpawnMode.Roller => _spawner.IsKindAllowed(ObstacleKind.Roller),
            _ => false
        };
    }

    private void HandleClickSpawn(ObstacleSpawnMode mode)
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        ObstacleKind kind = mode switch
        {
            ObstacleSpawnMode.Faller => ObstacleKind.Faller,
            ObstacleSpawnMode.Roller => ObstacleKind.Roller,
            _ => ObstacleKind.Faller
        };

        if (!TryGetSpawnAtViewportTop(Input.mousePosition, out Vector2 spawnPosition, out float aimWorldX))
            return;

        if (_spawner.TrySpawn(kind, spawnPosition, aimWorldX, _targetClimber))
            ReturnToFallerModeAfterHeavySpawn(kind);
    }

    private void HandleBouncerDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _bouncerDragActive = true;
            _bouncerDragStartScreen = Input.mousePosition;

            if (TryGetWorldXFromScreen(_bouncerDragStartScreen, out float aimX))
                _bouncerAimWorldX = aimX;

            UpdateBouncerAimLine(Input.mousePosition);
        }

        if (_bouncerDragActive && Input.GetMouseButton(0))
            UpdateBouncerAimLine(Input.mousePosition);

        if (!_bouncerDragActive || !Input.GetMouseButtonUp(0))
            return;

        Vector2 endScreen = Input.mousePosition;
        Vector2 delta = endScreen - _bouncerDragStartScreen;
        CancelBouncerDrag();

        if (delta.sqrMagnitude < _bouncerMinDragScreenPixels * _bouncerMinDragScreenPixels)
            return;

        if (!TryGetSpawnAtViewportTop(_bouncerDragStartScreen, out Vector2 spawnPosition, out _))
            return;

        if (!TryResolveBouncerLaunchDirection(delta, out Vector2 launchDirection))
            return;

        if (_spawner.TrySpawn(
                ObstacleKind.Bouncer,
                spawnPosition,
                _bouncerAimWorldX,
                _targetClimber,
                launchDirection))
        {
            ReturnToFallerModeAfterHeavySpawn(ObstacleKind.Bouncer);
        }
    }

    private void CancelBouncerDrag()
    {
        _bouncerDragActive = false;
        SetBouncerAimLineVisible(false);
    }

    private void UpdateBouncerAimLine(Vector2 currentScreenPosition)
    {
        if (_bouncerAimLine == null || !_bouncerDragActive)
            return;

        if (!TryGetSpawnAtViewportTop(_bouncerDragStartScreen, out Vector2 spawnPosition, out _))
        {
            SetBouncerAimLineVisible(false);
            return;
        }

        Vector2 screenDelta = currentScreenPosition - _bouncerDragStartScreen;
        Vector2 launchDirection = GetBouncerPreviewDirection(screenDelta);

        float lineZ = GetGameplayPlaneZ();
        Vector3 start = new Vector3(spawnPosition.x, spawnPosition.y, lineZ);
        Vector3 end = start + (Vector3)(launchDirection * _bouncerAimLineLength);
        end.z = lineZ;

        _bouncerAimLine.SetPosition(0, start);
        _bouncerAimLine.SetPosition(1, end);
        UpdateBouncerAimHeadLines(end, launchDirection, lineZ);
        SetBouncerAimLineVisible(true);
    }

    private void UpdateBouncerAimHeadLines(Vector3 tip, Vector2 launchDirection, float lineZ)
    {
        Vector2 backward = -launchDirection;
        float headAngleRad = _bouncerAimHeadAngleDegrees * Mathf.Deg2Rad;
        Vector2 leftWing = Rotate2D(backward, headAngleRad) * _bouncerAimHeadLength;
        Vector2 rightWing = Rotate2D(backward, -headAngleRad) * _bouncerAimHeadLength;

        Vector3 leftEnd = tip + (Vector3)leftWing;
        Vector3 rightEnd = tip + (Vector3)rightWing;
        leftEnd.z = lineZ;
        rightEnd.z = lineZ;

        if (_bouncerAimHeadLeft != null)
        {
            _bouncerAimHeadLeft.SetPosition(0, tip);
            _bouncerAimHeadLeft.SetPosition(1, leftEnd);
        }

        if (_bouncerAimHeadRight != null)
        {
            _bouncerAimHeadRight.SetPosition(0, tip);
            _bouncerAimHeadRight.SetPosition(1, rightEnd);
        }
    }

    private static Vector2 Rotate2D(Vector2 vector, float radians)
    {
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos);
    }

    private float GetGameplayPlaneZ() => GameplayPlaneZ;

    private void SetBouncerAimLineVisible(bool visible)
    {
        if (_bouncerAimLine != null)
            _bouncerAimLine.enabled = visible;

        if (_bouncerAimHeadLeft != null)
            _bouncerAimHeadLeft.enabled = visible;

        if (_bouncerAimHeadRight != null)
            _bouncerAimHeadRight.enabled = visible;
    }

    private Vector2 GetBouncerPreviewDirection(Vector2 screenDelta)
    {
        if (TryResolveBouncerLaunchDirection(screenDelta, out Vector2 launchDirection))
            return launchDirection;

        float minAngle = GetBouncerMinDownAngleDegrees();
        float signX = Mathf.Abs(screenDelta.x) > 0.01f ? Mathf.Sign(screenDelta.x) : 1f;
        return ClampDownwardFromHorizontal(new Vector2(signX, -Mathf.Tan(minAngle * Mathf.Deg2Rad)), minAngle);
    }

    private float GetBouncerMinDownAngleDegrees()
    {
        return _spawner.Tuning != null
            ? _spawner.Tuning.Bouncer.MinLaunchDownAngleFromHorizontalDegrees
            : 15f;
    }

    private void ReturnToFallerModeAfterHeavySpawn(ObstacleKind spawnedKind)
    {
        if (spawnedKind != ObstacleKind.Bouncer && spawnedKind != ObstacleKind.Roller)
            return;

        if (_spawner != null && _spawner.IsKindAllowed(ObstacleKind.Faller))
            _mode = ObstacleSpawnMode.Faller;
        else
            EnsureValidMode();
    }

    private bool TryGetSpawnAtViewportTop(Vector2 screenPosition, out Vector2 spawnPosition, out float aimWorldX)
    {
        spawnPosition = default;
        aimWorldX = 0f;

        if (!TryGetWorldXFromScreen(screenPosition, out aimWorldX))
            return false;

        float zDistance = GetCameraZDistance();
        Vector3 viewport = _camera.ScreenToViewportPoint(screenPosition);
        Vector3 topWorld = _camera.ViewportToWorldPoint(new Vector3(viewport.x, 1f, zDistance));
        topWorld.z = GetGameplayPlaneZ();

        spawnPosition = new Vector2(aimWorldX, topWorld.y);
        return true;
    }

    private bool TryGetWorldXFromScreen(Vector2 screenPosition, out float worldX)
    {
        worldX = 0f;
        float zDistance = GetCameraZDistance();
        Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, zDistance));
        world.z = GetGameplayPlaneZ();
        worldX = world.x;
        return true;
    }

    private void OnDestroy()
    {
        if (_bouncerAimLineMaterial != null)
            Destroy(_bouncerAimLineMaterial);
    }

    private float GetCameraZDistance() =>
        Mathf.Abs(_camera.transform.position.z - GetGameplayPlaneZ());

    private bool TryResolveBouncerLaunchDirection(Vector2 screenDelta, out Vector2 launchDirection)
    {
        launchDirection = default;

        Vector2 worldStart = ScreenToWorld2D(_bouncerDragStartScreen);
        Vector2 worldEnd = ScreenToWorld2D(_bouncerDragStartScreen + screenDelta);
        Vector2 raw = worldEnd - worldStart;

        if (raw.sqrMagnitude < MinDirectionSqrMagnitude)
            return false;

        float minAngle = GetBouncerMinDownAngleDegrees();
        launchDirection = ClampDownwardFromHorizontal(raw, minAngle);
        return launchDirection.sqrMagnitude >= MinDirectionSqrMagnitude;
    }

    private Vector2 ScreenToWorld2D(Vector2 screenPosition)
    {
        float zDistance = GetCameraZDistance();
        Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, zDistance));
        world.z = GetGameplayPlaneZ();
        return world;
    }

    /// <summary>World-space direction: at least <paramref name="minDownAngleDegrees"/> below the horizontal.</summary>
    private static Vector2 ClampDownwardFromHorizontal(Vector2 raw, float minDownAngleDegrees)
    {
        float minTan = Mathf.Tan(minDownAngleDegrees * Mathf.Deg2Rad);
        float dx = raw.x;
        float dy = raw.y;

        if (dy > -0.0001f)
        {
            float signX = Mathf.Abs(dx) > 0.0001f ? Mathf.Sign(dx) : 1f;
            dx = signX;
            dy = -minTan;
        }
        else
        {
            float absDx = Mathf.Abs(dx);
            float absDy = Mathf.Abs(dy);
            if (absDx > 0.0001f && absDy / absDx < minTan)
            {
                float signX = Mathf.Sign(dx);
                dx = signX;
                dy = -minTan;
            }
        }

        return new Vector2(dx, dy).normalized;
    }

    private enum ObstacleSpawnMode
    {
        Faller,
        Bouncer,
        Roller
    }
}
