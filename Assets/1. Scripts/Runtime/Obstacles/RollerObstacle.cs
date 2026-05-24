using UnityEngine;

/// <summary>
/// Lands on Map, rolls with a fixed direction until the next air → grounded landing.
/// Direction is chosen once per landing toward the injected roll target X, else spawn-time aim.
/// Left/right wall contact flips horizontal roll direction only (not player aim).
/// </summary>
public sealed class RollerObstacle : ObstacleBase
{
    private Vector2 _velocity;
    private bool _wasOnPlatform;
    private int _rollDirection = 1;

    public override ObstacleKind Kind => ObstacleKind.Roller;

    protected override void OnActivated()
    {
        _velocity = Vector2.zero;
        _wasOnPlatform = false;
        _rollDirection = 1;
        ApplyVelocity(_velocity);
    }

    protected override void OnFixedTick(float deltaTime)
    {
        if(transform.position.y < -11f)
        {
            ReleaseToPool();
            return;
        }

        if (Tuning == null)
            return;

        var tuning = Tuning.Roller;

        if (TryGetPlatformHit(out var hit))
        {
            if (!_wasOnPlatform)
                _rollDirection = ResolveRollDirectionAtLanding();

            _wasOnPlatform = true;
            _velocity.x = _rollDirection * tuning.RollSpeed;
            _velocity.y = 0f;

            float radius = tuning.GroundCheckRadius;
            var position = (Vector2)transform.position;
            TryApplyHorizontalMove(ref position, deltaTime, radius);
            position.y = hit.point.y + radius;
            ApplyPosition(position);
        }
        else
        {
            _wasOnPlatform = false;

            float gravity = Physics2D.gravity.y * tuning.GravityScale;
            _velocity.y += gravity * deltaTime;

            var position = (Vector2)transform.position;
            TryApplyHorizontalMove(ref position, deltaTime, tuning.GroundCheckRadius);
            position.y += _velocity.y * deltaTime;
            ApplyPosition(position);
        }

        ApplyVelocity(_velocity);
    }

    private void TryApplyHorizontalMove(ref Vector2 position, float deltaTime, float castRadius)
    {
        float deltaX = _velocity.x * deltaTime;
        if (Mathf.Abs(deltaX) < 0.000001f)
            return;

        var tuning = Tuning.Roller;
        if (tuning.WallLayers.value == 0)
        {
            position.x += deltaX;
            return;
        }

        Vector2 direction = deltaX > 0f ? Vector2.right : Vector2.left;
        float distance = Mathf.Abs(deltaX);

        var hit = Physics2D.CircleCast(
            position,
            castRadius,
            direction,
            distance,
            tuning.WallLayers);

        if (hit.collider == null || hit.collider.isTrigger || Mathf.Abs(hit.normal.x) < 0.5f)
        {
            position.x += deltaX;
            return;
        }

        _rollDirection = -_rollDirection;
        _velocity.x = _rollDirection * tuning.RollSpeed;
        position = (Vector2)hit.point + hit.normal * (castRadius + tuning.SurfaceSkin);
    }

    private float GetRollTargetWorldXAtLanding()
    {
        if (RollTargetAgent != null)
            return RollTargetAgent.WorldPosition.x;

        return PlayerAimWorldX;
    }

    private int ResolveRollDirectionAtLanding()
    {
        float targetX = GetRollTargetWorldXAtLanding();
        float delta = targetX - transform.position.x;
        float deadZone = Tuning.Roller.AimXDeadZone;

        if (delta > deadZone)
            return 1;
        if (delta < -deadZone)
            return -1;

        return _rollDirection != 0 ? _rollDirection : 1;
    }

    private bool TryGetPlatformHit(out RaycastHit2D hit)
    {
        hit = default;
        if (Tuning == null)
            return false;

        var tuning = Tuning.Roller;
        var origin = (Vector2)transform.position;
        float radius = tuning.GroundCheckRadius;
        float distance = tuning.GroundRayDistance;

        hit = Physics2D.CircleCast(
            origin,
            radius,
            Vector2.down,
            distance,
            tuning.PlatformLayers);

        if (hit.collider != null && !hit.collider.isTrigger)
            return true;

        var feet = origin + Vector2.down * radius;
        hit = Physics2D.Raycast(feet, Vector2.down, distance, tuning.PlatformLayers);
        return hit.collider != null && !hit.collider.isTrigger;
    }
}
