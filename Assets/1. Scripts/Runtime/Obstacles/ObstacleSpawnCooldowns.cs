using System;
using UnityEngine;

/// <summary>
/// Per-<see cref="ObstacleKind"/> spawn cooldown timers driven by <see cref="ObstacleTuningConfig"/>.
/// </summary>
public sealed class ObstacleSpawnCooldowns
{
    private readonly float[] _cooldownDurations;
    private readonly float[] _readyAt;

    public ObstacleSpawnCooldowns(float fallerCooldown, float bouncerCooldown, float rollerCooldown)
    {
        _readyAt = new float[Enum.GetValues(typeof(ObstacleKind)).Length];
        _cooldownDurations = new float[_readyAt.Length];
        _cooldownDurations[(int)ObstacleKind.Faller] = fallerCooldown;
        _cooldownDurations[(int)ObstacleKind.Bouncer] = bouncerCooldown;
        _cooldownDurations[(int)ObstacleKind.Roller] = rollerCooldown;
    }

    public bool IsReady(ObstacleKind kind)
    {
        return Time.time >= _readyAt[(int)kind];
    }

    public float RemainingSeconds(ObstacleKind kind)
    {
        return Mathf.Max(0f, _readyAt[(int)kind] - Time.time);
    }

    public float CooldownDuration(ObstacleKind kind)
    {
        return _cooldownDurations[(int)kind];
    }

    /// <summary>Starts cooldown after a successful spawn.</summary>
    public void CommitSpawn(ObstacleKind kind)
    {
        _readyAt[(int)kind] = Time.time + _cooldownDurations[(int)kind];
    }

    public void ResetAll()
    {
        for (int i = 0; i < _readyAt.Length; i++)
            _readyAt[i] = 0f;
    }
}
