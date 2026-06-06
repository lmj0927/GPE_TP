using System;
using UnityEngine;

/// <summary>
/// Villain input: Alpha1/2/3 select spawn mode; left click spawns Faller/Roller at viewport top X.
/// Bouncer mode: click locks spawn at viewport-top arrow position; drag/release aim uses spawn-to-mouse world direction.
/// </summary>
public sealed class PlayerObstacleInput : MonoBehaviour
{
    private const float MinDirectionSqrMagnitude = 0.0001f;
    private const float GameplayPlaneZ = 0f;
    private const float DefaultArrowFacingAngleDegrees = 0f;
    private static readonly Vector2 BouncerIdleDirection = Vector2.down;

    [SerializeField] private PlayerObstacleSpawner _spawner;
    [SerializeField] private MonoBehaviour _targetClimberBehaviour;
    [SerializeField] private Camera _camera;
    [SerializeField, Min(0f)] private float _bouncerMinDragScreenPixels = 8f;
    [SerializeField] private GameObject _bouncerAimArrow;
    [SerializeField, Min(0f)] private float _bouncerAimArrowOffset = 0.5f;

    private IClimberAgent _targetClimber;
    private ObstacleSpawnMode _mode = ObstacleSpawnMode.Faller;
    private bool _bouncerDragActive;

    public ObstacleKind CurrentObstacleKind => ToObstacleKind(_mode);

    public event Action<ObstacleKind> ObstacleKindChanged;

    public bool TryGetSpawnPositionFromScreen(Vector2 screenPosition, out Vector2 worldSpawnPosition)
    {
        if (_camera == null)
        {
            worldSpawnPosition = default;
            return false;
        }

        return TryGetSpawnAtViewportTop(screenPosition, out worldSpawnPosition, out _);
    }
    private Vector2 _bouncerDragStartScreen;
    private float _bouncerAimWorldX;

    private void Awake()
    {
        if (_camera == null)
            _camera = Camera.main;

        if (_targetClimberBehaviour != null)
            _targetClimberBehaviour.TryGetComponent(out _targetClimber);

        SetBouncerAimArrowVisible(false);
        EnsureValidMode();
        NotifyObstacleKindChanged();
    }

    private void Update()
    {
        if (_spawner == null || _camera == null)
            return;

        HandleModeKeys();

        if (_mode != ObstacleSpawnMode.Bouncer && _bouncerDragActive)
            CancelBouncerDrag();

        if (_mode == ObstacleSpawnMode.Bouncer)
        {
            HandleBouncerDrag();
            if (!_bouncerDragActive)
                UpdateBouncerIdleArrow();
        }
        else
        {
            SetBouncerAimArrowVisible(false);
            HandleClickSpawn(_mode);
        }
    }

    private void HandleModeKeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) && _spawner.IsKindAllowed(ObstacleKind.Faller))
            SetMode(ObstacleSpawnMode.Faller);

        if (Input.GetKeyDown(KeyCode.Alpha2) &&
            _spawner.IsKindAllowed(ObstacleKind.Bouncer) &&
            _spawner.IsReady(ObstacleKind.Bouncer))
        {
            SetMode(ObstacleSpawnMode.Bouncer);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3) &&
            _spawner.IsKindAllowed(ObstacleKind.Roller) &&
            _spawner.IsReady(ObstacleKind.Roller))
        {
            SetMode(ObstacleSpawnMode.Roller);
        }
    }

    private void EnsureValidMode()
    {
        if (_spawner == null)
            return;

        if (IsModeAllowed(_mode))
            return;

        if (_spawner.IsKindAllowed(ObstacleKind.Faller))
            SetMode(ObstacleSpawnMode.Faller);
        else if (_spawner.IsKindAllowed(ObstacleKind.Bouncer))
            SetMode(ObstacleSpawnMode.Bouncer);
        else if (_spawner.IsKindAllowed(ObstacleKind.Roller))
            SetMode(ObstacleSpawnMode.Roller);
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

            UpdateBouncerAimArrow(Input.mousePosition);
        }

        if (_bouncerDragActive && Input.GetMouseButton(0))
            UpdateBouncerAimArrow(Input.mousePosition);

        if (!_bouncerDragActive || !Input.GetMouseButtonUp(0))
            return;

        Vector2 endScreen = Input.mousePosition;
        Vector2 delta = endScreen - _bouncerDragStartScreen;
        CancelBouncerDrag();

        if (delta.sqrMagnitude < _bouncerMinDragScreenPixels * _bouncerMinDragScreenPixels)
            return;

        if (!TryGetSpawnAtViewportTop(_bouncerDragStartScreen, out Vector2 spawnPosition, out _))
            return;

        if (!TryResolveBouncerLaunchDirection(spawnPosition, endScreen, out Vector2 launchDirection))
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
    }

    private void UpdateBouncerIdleArrow()
    {
        if (_bouncerAimArrow == null)
            return;

        if (!TryGetSpawnAtViewportTop(Input.mousePosition, out Vector2 spawnPosition, out _))
        {
            SetBouncerAimArrowVisible(false);
            return;
        }

        ApplyBouncerAimArrow(spawnPosition, BouncerIdleDirection);
    }

    private void UpdateBouncerAimArrow(Vector2 currentScreenPosition)
    {
        if (_bouncerAimArrow == null || !_bouncerDragActive)
            return;

        if (!TryGetSpawnAtViewportTop(_bouncerDragStartScreen, out Vector2 spawnPosition, out _))
        {
            SetBouncerAimArrowVisible(false);
            return;
        }

        Vector2 launchDirection = GetBouncerPreviewDirection(spawnPosition, currentScreenPosition);
        ApplyBouncerAimArrow(spawnPosition, launchDirection);
    }

    private void ApplyBouncerAimArrow(Vector2 spawnPosition, Vector2 launchDirection)
    {
        float z = GetGameplayPlaneZ();
        Vector3 position = new Vector3(spawnPosition.x, spawnPosition.y, z);

        if (_bouncerAimArrowOffset > 0f)
            position += (Vector3)(launchDirection.normalized * _bouncerAimArrowOffset);

        _bouncerAimArrow.transform.position = position;
        _bouncerAimArrow.transform.rotation = Quaternion.Euler(
            0f,
            0f,
            GetArrowRotationZ(launchDirection));

        SetBouncerAimArrowVisible(true);
    }

    /// <summary>Arrow art faces right (+X) at 0°. Rotates to match launch direction.</summary>
    private static float GetArrowRotationZ(Vector2 direction)
    {
        if (direction.sqrMagnitude < MinDirectionSqrMagnitude)
            return DefaultArrowFacingAngleDegrees;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        return angle + DefaultArrowFacingAngleDegrees;
    }

    private void SetBouncerAimArrowVisible(bool visible)
    {
        if (_bouncerAimArrow == null)
            return;

        if (_bouncerAimArrow.activeSelf != visible)
            _bouncerAimArrow.SetActive(visible);
    }

    private Vector2 GetBouncerPreviewDirection(Vector2 spawnPosition, Vector2 aimScreenPosition)
    {
        if (TryResolveBouncerLaunchDirection(spawnPosition, aimScreenPosition, out Vector2 launchDirection))
            return launchDirection;

        Vector2 aimWorld = ScreenToWorld2D(aimScreenPosition);
        float minAngle = GetBouncerMinDownAngleDegrees();
        float signX = Mathf.Abs(aimWorld.x - spawnPosition.x) > 0.01f
            ? Mathf.Sign(aimWorld.x - spawnPosition.x)
            : 1f;
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
            SetMode(ObstacleSpawnMode.Faller);
        else
            EnsureValidMode();
    }

    private void SetMode(ObstacleSpawnMode mode)
    {
        if (_mode == mode)
            return;

        _bouncerDragActive = false;
        SetBouncerAimArrowVisible(false);
        _mode = mode;
        NotifyObstacleKindChanged();
    }

    private void NotifyObstacleKindChanged()
    {
        ObstacleKindChanged?.Invoke(CurrentObstacleKind);
    }

    private static ObstacleKind ToObstacleKind(ObstacleSpawnMode mode)
    {
        return mode switch
        {
            ObstacleSpawnMode.Faller => ObstacleKind.Faller,
            ObstacleSpawnMode.Bouncer => ObstacleKind.Bouncer,
            ObstacleSpawnMode.Roller => ObstacleKind.Roller,
            _ => ObstacleKind.Faller
        };
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

    private float GetGameplayPlaneZ() => GameplayPlaneZ;

    private float GetCameraZDistance() =>
        Mathf.Abs(_camera.transform.position.z - GetGameplayPlaneZ());

    private bool TryResolveBouncerLaunchDirection(
        Vector2 spawnPosition,
        Vector2 aimScreenPosition,
        out Vector2 launchDirection)
    {
        launchDirection = default;

        Vector2 aimWorld = ScreenToWorld2D(aimScreenPosition);
        Vector2 raw = aimWorld - spawnPosition;

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
