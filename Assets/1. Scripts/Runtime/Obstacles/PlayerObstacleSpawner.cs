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
    private float _fallerSpeed;
    private float _bouncerSpeed;
    private float _rollerSpeed;

    public int SpawnCount => _spawnCount;

    private void Awake()
    {
        CacheUpgradeAdjustedTuning();
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

        var obstacle = _obstaclePool.Rent(
            kind,
            worldPosition,
            playerAimWorldX,
            GetRuntimeSpeed(kind),
            targetClimber,
            launchDirection);
        if (obstacle == null)
            return false;

        _cooldowns.CommitSpawn(kind);
        _spawnCount++;
        AudioManager.TryPlay(AudioType.Spawn);
        return true;
    }

    public void ResetCooldowns() => _cooldowns?.ResetAll();

    public void ReleaseAllActiveObstacles() => _obstaclePool?.ReleaseAllActive();

    public void ResetForEpisode()
    {
        ReleaseAllActiveObstacles();
        ResetCooldowns();
    }

    private void CacheUpgradeAdjustedTuning()
    {
        if (_tuning == null)
            return;

        UserData data = UserDataStore.Load();
        int up1 = Mathf.Clamp(data.Upgrade1, 0, 10);
        int up2 = Mathf.Clamp(data.Upgrade2, 0, 10);
        int up3 = Mathf.Clamp(data.Upgrade3, 0, 10);

        _fallerSpeed = _tuning.Faller.FallSpeed + _tuning.Faller.FallSpeedPerUpgradeLevel * up1;
        _bouncerSpeed = _tuning.Bouncer.LaunchSpeed + _tuning.Bouncer.LaunchSpeedPerUpgradeLevel * up2;
        _rollerSpeed = _tuning.Roller.RollSpeed + _tuning.Roller.RollSpeedPerUpgradeLevel * up3;

        float fallerCooldown = _tuning.Faller.SpawnCooldownSeconds -
                               _tuning.Faller.SpawnCooldownDecreasePerUpgradeLevel * up1;
        float bouncerCooldown = _tuning.Bouncer.SpawnCooldownSeconds -
                                _tuning.Bouncer.SpawnCooldownDecreasePerUpgradeLevel * up2;
        float rollerCooldown = _tuning.Roller.SpawnCooldownSeconds -
                               _tuning.Roller.SpawnCooldownDecreasePerUpgradeLevel * up3;

        _cooldowns = new ObstacleSpawnCooldowns(fallerCooldown, bouncerCooldown, rollerCooldown);
    }

    private float GetRuntimeSpeed(ObstacleKind kind)
    {
        return kind switch
        {
            ObstacleKind.Faller => _fallerSpeed,
            ObstacleKind.Bouncer => _bouncerSpeed,
            ObstacleKind.Roller => _rollerSpeed,
            _ => 0f
        };
    }
}
