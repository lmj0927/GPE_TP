using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ObstacleTuningConfig", menuName = "GPE/Obstacles/Tuning Config")]
public sealed class ObstacleTuningConfig : ScriptableObject
{
    [SerializeField] private LayerMask _enemyLayers;
    [SerializeField] private float _recycleBelowWorldY = -30f;
    [SerializeField] private float _recycleBelowTargetOffsetY = 3f;
    [SerializeField] private FallerTuning _faller = new();
    [SerializeField] private BouncerTuning _bouncer = new();
    [SerializeField] private RollerTuning _roller = new();

    public LayerMask EnemyLayers => _enemyLayers;
    public float RecycleBelowWorldY => _recycleBelowWorldY;
    public float RecycleBelowTargetOffsetY => _recycleBelowTargetOffsetY;
    public FallerTuning Faller => _faller;
    public BouncerTuning Bouncer => _bouncer;
    public RollerTuning Roller => _roller;

    public float GetSpawnCooldown(ObstacleKind kind)
    {
        return kind switch
        {
            ObstacleKind.Faller => _faller.SpawnCooldownSeconds,
            ObstacleKind.Bouncer => _bouncer.SpawnCooldownSeconds,
            ObstacleKind.Roller => _roller.SpawnCooldownSeconds,
            _ => 0f
        };
    }

    [Serializable]
    public sealed class FallerTuning
    {
        [SerializeField] private float _fallSpeed = 10f;
        [SerializeField] private float _spawnCooldownSeconds = 0.8f;

        public float FallSpeed => _fallSpeed;
        public float SpawnCooldownSeconds => _spawnCooldownSeconds;
    }

    [Serializable]
    public sealed class BouncerTuning
    {
        [SerializeField] private float _launchSpeed = 12f;
        [SerializeField] private float _launchAngleDegrees = 315f;
        [SerializeField] private LayerMask _wallLayers;
        [SerializeField] private float _castRadius = 0.5f;
        [SerializeField] private float _surfaceSkin = 0.02f;
        [SerializeField] private float _wallBounceDamping = 1f;
        [SerializeField] private float _spawnCooldownSeconds = 2f;
        [SerializeField, Range(0f, 89f)] private float _minLaunchDownAngleFromHorizontalDegrees = 15f;

        public float LaunchSpeed => _launchSpeed;
        public float MinLaunchDownAngleFromHorizontalDegrees => _minLaunchDownAngleFromHorizontalDegrees;
        public float LaunchAngleDegrees => _launchAngleDegrees;
        public LayerMask WallLayers => _wallLayers;
        public float CastRadius => _castRadius;
        public float SurfaceSkin => _surfaceSkin;
        public float WallBounceDamping => _wallBounceDamping;
        public float SpawnCooldownSeconds => _spawnCooldownSeconds;
    }

    [Serializable]
    public sealed class RollerTuning
    {
        [SerializeField] private float _rollSpeed = 7f;
        [SerializeField] private float _gravityScale = 2f;
        [SerializeField] private LayerMask _platformLayers;
        [SerializeField] private float _groundRayDistance = 0.25f;
        [SerializeField] private float _groundCheckRadius = 0.2f;
        [SerializeField] private float _aimXDeadZone = 0.15f;
        [SerializeField] private LayerMask _wallLayers;
        [SerializeField] private float _surfaceSkin = 0.02f;
        [SerializeField] private float _spawnCooldownSeconds = 3f;

        public float RollSpeed => _rollSpeed;
        public float GravityScale => _gravityScale;
        public LayerMask PlatformLayers => _platformLayers;
        public float GroundRayDistance => _groundRayDistance;
        public float GroundCheckRadius => _groundCheckRadius;
        public float AimXDeadZone => _aimXDeadZone;
        public LayerMask WallLayers => _wallLayers;
        public float SurfaceSkin => _surfaceSkin;
        public float SpawnCooldownSeconds => _spawnCooldownSeconds;
    }
}
