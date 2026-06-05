using UnityEngine;

/// <summary>
/// Villain obstacle spawn entry point. Enforces per-type cooldown from <see cref="ObstacleTuningConfig"/>.
/// </summary>
public sealed class PlayerObstacleSpawner : MonoBehaviour
{
    [SerializeField] private ObstaclePool _obstaclePool;
    [SerializeField] private ObstacleTuningConfig _tuning;

    [Header("Allowed types (per scene / stage)")]
    [SerializeField] private bool _fallerAllowed = true;
    [SerializeField] private bool _bouncerAllowed = true;
    [SerializeField] private bool _rollerAllowed = true;

    public ObstacleTuningConfig Tuning => _tuning;

    private ObstacleSpawnCooldowns _cooldowns;
    private int _spawnCount;

    public int SpawnCount => _spawnCount;

    private void Awake()
    {
        _cooldowns = new ObstacleSpawnCooldowns(_tuning);
    }

    public bool IsKindAllowed(ObstacleKind kind)
    {
        return kind switch
        {
            ObstacleKind.Faller => _fallerAllowed,
            ObstacleKind.Bouncer => _bouncerAllowed,
            ObstacleKind.Roller => _rollerAllowed,
            _ => false
        };
    }

    public bool IsReady(ObstacleKind kind) => _cooldowns != null && _cooldowns.IsReady(kind);

    public float RemainingCooldown(ObstacleKind kind)
    {
        return _cooldowns != null ? _cooldowns.RemainingSeconds(kind) : 0f;
    }

    public float CooldownDuration(ObstacleKind kind)
    {
        return _cooldowns != null ? _cooldowns.CooldownDuration(kind) : 0f;
    }

    /// <summary>
    /// Spawns when off cooldown and pool rent succeeds. Returns false if on cooldown or rent failed.
    /// </summary>
    public bool TrySpawn(
        ObstacleKind kind,
        Vector2 worldPosition,
        float playerAimWorldX,
        IClimberAgent targetClimber = null,
        Vector2? launchDirection = null)
    {
        if (!IsKindAllowed(kind) || _obstaclePool == null || _cooldowns == null || !_cooldowns.IsReady(kind))
            return false;

        var obstacle = _obstaclePool.Rent(kind, worldPosition, playerAimWorldX, targetClimber, launchDirection);
        if (obstacle == null)
            return false;

        _cooldowns.CommitSpawn(kind);
        _spawnCount++;
        return true;
    }

    public void ResetCooldowns() => _cooldowns?.ResetAll();

    public void ReleaseAllActiveObstacles() => _obstaclePool?.ReleaseAllActive();

    public void ResetForEpisode()
    {
        ReleaseAllActiveObstacles();
        ResetCooldowns();
    }
}
