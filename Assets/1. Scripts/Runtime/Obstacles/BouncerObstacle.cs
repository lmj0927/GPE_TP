using UnityEngine;

/// <summary>
/// Kinematic constant-speed flight. Wall bounce via CircleCast (same approach as Roller horizontal sweep).
/// </summary>
public sealed class BouncerObstacle : ObstacleBase
{
    private const float SideWallNormalMinAbsX = 0.5f;
    private const float MinMoveDistance = 0.000001f;

    private Vector2 _direction;
    private float _speed;

    public override ObstacleKind Kind => ObstacleKind.Bouncer;

    protected override void OnActivated()
    {
        if (Tuning == null)
            return;

        var tuning = Tuning.Bouncer;
        _speed = tuning.LaunchSpeed;

        if (SpawnLaunchDirection is { } launchDirection && launchDirection.sqrMagnitude > 0.0001f)
            _direction = launchDirection.normalized;
        else
        {
            float rad = tuning.LaunchAngleDegrees * Mathf.Deg2Rad;
            _direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
        }

        ApplyVelocity(_direction * _speed);
    }

    protected override void OnFixedTick(float deltaTime)
    {
        if (Tuning == null)
            return;

        var tuning = Tuning.Bouncer;
        var position = (Vector2)transform.position;
        var displacement = _direction * _speed * deltaTime;

        TryApplyMoveWithWallBounce(ref position, displacement, tuning);
        ApplyPosition(position);
        ApplyVelocity(_direction * _speed);
    }

    private void TryApplyMoveWithWallBounce(
        ref Vector2 position,
        Vector2 displacement,
        ObstacleTuningConfig.BouncerTuning tuning)
    {
        float distance = displacement.magnitude;
        if (distance < MinMoveDistance)
            return;

        if (tuning.WallLayers.value == 0)
        {
            position += displacement;
            return;
        }

        var hit = Physics2D.CircleCast(
            position,
            tuning.CastRadius,
            displacement / distance,
            distance,
            tuning.WallLayers);

        if (hit.collider == null || hit.collider.isTrigger || Mathf.Abs(hit.normal.x) < SideWallNormalMinAbsX)
        {
            position += displacement;
            return;
        }

        float awaySign = Mathf.Sign(hit.normal.x);
        if (_direction.x * awaySign > 0f)
        {
            position += displacement;
            return;
        }

        _direction.x = Mathf.Abs(_direction.x) * awaySign;
        _direction = _direction.normalized;
        _speed *= tuning.WallBounceDamping;

        position = (Vector2)hit.point + hit.normal * (tuning.CastRadius + tuning.SurfaceSkin);
    }
}
