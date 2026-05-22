using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Training spawner: each frame spawns every obstacle type that is off cooldown (no spawn interval).
/// </summary>
public sealed class HeuristicSpawnerBot : MonoBehaviour
{
    private const float DownLaunchAngleDegrees = 270f;

    [SerializeField] private EnemyAgentVer2 _climber;
    [SerializeField] private PlayerObstacleSpawner _spawner;
    [SerializeField] private SpawnerPatternConfig _pattern;

    private static readonly ObstacleKind[] AllKinds =
        (ObstacleKind[])Enum.GetValues(typeof(ObstacleKind));

    public void ResetForEpisode() => _spawner?.ResetForEpisode();

    private void Update()
    {
        if (_climber == null || _spawner == null || _pattern == null)
            return;

        Vector2 climberPos = _climber.WorldPosition;
        Vector2 climberVel = _climber.WorldVelocity;
        float aimX = climberPos.x + climberVel.x * _pattern.AimLeadSeconds;
        var spawnPosition = new Vector2(aimX, climberPos.y + _pattern.SpawnHeightOffset);

        for (int i = 0; i < AllKinds.Length; i++)
        {
            ObstacleKind kind = AllKinds[i];
            if (!_spawner.IsReady(kind))
                continue;

            Vector2? launchDirection = kind == ObstacleKind.Bouncer
                ? SampleRandomDownwardBouncerDirection()
                : null;

            _spawner.TrySpawn(kind, spawnPosition, aimX, _climber, launchDirection);
        }
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
