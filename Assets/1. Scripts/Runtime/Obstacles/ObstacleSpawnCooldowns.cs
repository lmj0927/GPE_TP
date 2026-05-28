using System;
using UnityEngine;

/// <summary>
/// Per-<see cref="ObstacleKind"/> spawn cooldown timers driven by <see cref="ObstacleTuningConfig"/>.
/// </summary>
public sealed class ObstacleSpawnCooldowns
{
    private readonly ObstacleTuningConfig _config;
    private readonly float[] _readyAt;

    public ObstacleSpawnCooldowns(ObstacleTuningConfig config)
    {
        _config = config;
        _readyAt = new float[Enum.GetValues(typeof(ObstacleKind)).Length];
    }

    public bool IsReady(ObstacleKind kind)
    {
        if (_config == null)
            return false;

        return Time.time >= _readyAt[(int)kind];
    }

    public float RemainingSeconds(ObstacleKind kind)
    {
        return Mathf.Max(0f, _readyAt[(int)kind] - Time.time);
    }

    public float CooldownDuration(ObstacleKind kind)
    {
        return _config != null ? _config.GetSpawnCooldown(kind) : 0f;
    }

    /// <summary>Starts cooldown after a successful spawn.</summary>
    public void CommitSpawn(ObstacleKind kind)
    {
        if (_config == null)
            return;

        _readyAt[(int)kind] = Time.time + _config.GetSpawnCooldown(kind);
    }

    public void ResetAll()
    {
        for (int i = 0; i < _readyAt.Length; i++)
            _readyAt[i] = 0f;
    }
}
