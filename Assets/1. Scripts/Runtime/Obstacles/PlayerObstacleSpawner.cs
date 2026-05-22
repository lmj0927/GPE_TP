using UnityEngine;

/// <summary>
/// Villain obstacle spawn entry point. Enforces per-type cooldown from <see cref="ObstacleTuningConfig"/>.
/// Does not affect <see cref="ObstacleSpawnTest"/>.
/// </summary>
public sealed class PlayerObstacleSpawner : MonoBehaviour
{
    [SerializeField] private ObstaclePool _obstaclePool;
    [SerializeField] private ObstacleTuningConfig _tuning;

    private ObstacleSpawnCooldowns _cooldowns;

    private void Awake()
    {
        _cooldowns = new ObstacleSpawnCooldowns(_tuning);
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
        IClimberAgent rollTargetAgent = null,
        Vector2? launchDirection = null)
    {
        if (_obstaclePool == null || _cooldowns == null || !_cooldowns.IsReady(kind))
            return false;

        var obstacle = _obstaclePool.Rent(kind, worldPosition, playerAimWorldX, rollTargetAgent, launchDirection);
        if (obstacle == null)
            return false;

        _cooldowns.CommitSpawn(kind);
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
