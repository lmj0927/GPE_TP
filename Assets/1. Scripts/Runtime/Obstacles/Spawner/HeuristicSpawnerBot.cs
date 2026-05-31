using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Training spawner: one spawn attempt per <see cref="SpawnerPatternConfig.SpawnIntervalSeconds"/>.
/// Ready types compete by longest per-type cooldown duration (Roller &gt; Bouncer &gt; Faller).
/// Which kinds can spawn is set on <see cref="SpawnerPatternConfig"/> (Faller-only curriculum, etc.).
/// </summary>
public sealed class HeuristicSpawnerBot : MonoBehaviour
{
    private const float DownLaunchAngleDegrees = 270f;

    [SerializeField] private EnemyAgentVer2 _climber;
    [SerializeField] private PlayerObstacleSpawner _spawner;
    [SerializeField] private SpawnerPatternConfig _pattern;

    private static readonly ObstacleKind[] AllKinds =
        (ObstacleKind[])Enum.GetValues(typeof(ObstacleKind));

    private Coroutine _spawnLoop;

    private void OnEnable() => RestartSpawnLoop();

    private void OnDisable() => StopSpawnLoop();

    public void ResetForEpisode()
    {
        _spawner?.ResetForEpisode();
        if (isActiveAndEnabled)
            RestartSpawnLoop();
    }

    private void RestartSpawnLoop()
    {
        StopSpawnLoop();
        _spawnLoop = StartCoroutine(SpawnLoop());
    }

    private void StopSpawnLoop()
    {
        if (_spawnLoop == null)
            return;

        StopCoroutine(_spawnLoop);
        _spawnLoop = null;
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            float interval = _pattern != null ? _pattern.SpawnIntervalSeconds : 1f;
            yield return new WaitForSeconds(interval);
            TrySpawnAttempt();
        }
    }

    private void TrySpawnAttempt()
    {
        if (_climber == null || _spawner == null || _pattern == null)
            return;

        if (!TryPickReadyKindWithLongestCooldown(out ObstacleKind kind))
            return;

        Vector2 climberPos = _climber.WorldPosition;
        Vector2 climberVel = _climber.WorldVelocity;
        float aimX = climberPos.x + climberVel.x * _pattern.AimLeadSeconds;
        var spawnPosition = new Vector2(aimX, climberPos.y + _pattern.SpawnHeightOffset);

        Vector2? launchDirection = kind == ObstacleKind.Bouncer
            ? SampleRandomDownwardBouncerDirection()
            : null;

        _spawner.TrySpawn(kind, spawnPosition, aimX, _climber, launchDirection);
    }

    /// <summary>Among off-cooldown kinds, picks the one with the longest configured spawn cooldown.</summary>
    private bool TryPickReadyKindWithLongestCooldown(out ObstacleKind kind)
    {
        kind = default;
        float bestCooldown = -1f;
        bool found = false;

        for (int i = 0; i < AllKinds.Length; i++)
        {
            ObstacleKind candidate = AllKinds[i];
            if (!_pattern.IsKindEnabled(candidate))
                continue;

            if (!_spawner.IsReady(candidate))
                continue;

            float cooldown = _spawner.CooldownDuration(candidate);
            if (cooldown <= bestCooldown)
                continue;

            bestCooldown = cooldown;
            kind = candidate;
            found = true;
        }

        return found;
    }

    private Vector2 SampleRandomDownwardBouncerDirection()
    {
        float minTilt = _pattern.BouncerMinDownTiltDegrees;
        float maxTilt = _pattern.BouncerMaxDownTiltDegrees;
        if (maxTilt < minTilt)
            (minTilt, maxTilt) = (maxTilt, minTilt);

        float tilt = Random.Range(minTilt, maxTilt);
        float side = Random.value < 0.5f ? -1f : 1f;
        float angleDeg = DownLaunchAngleDegrees + side * tilt;
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }
}
