using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pools Faller / Bouncer / Roller prefabs via <see cref="ObjectPool{T}"/>.
/// </summary>
public sealed class ObstaclePool : MonoBehaviour
{
    [Serializable]
    private sealed class PoolEntry
    {
        [SerializeField] private ObstacleKind _kind;
        [SerializeField] private ObstacleBase _prefab;
        [SerializeField] private int _prewarmCount = 4;

        public ObstacleKind Kind => _kind;
        public ObstacleBase Prefab => _prefab;
        public int PrewarmCount => _prewarmCount;
    }

    [SerializeField] private Transform _poolRoot;
    [SerializeField] private PoolEntry[] _entries = Array.Empty<PoolEntry>();

    private readonly Dictionary<ObstacleKind, ObjectPool<ObstacleBase>> _pools = new();

    private void Awake()
    {
        if (_poolRoot == null)
            _poolRoot = transform;

        for (int i = 0; i < _entries.Length; i++)
        {
            var entry = _entries[i];
            if (entry.Prefab == null)
                continue;

            _pools[entry.Kind] = new ObjectPool<ObstacleBase>(
                entry.Prefab,
                _poolRoot,
                entry.PrewarmCount);
        }
    }

    public ObstacleBase Rent(
        ObstacleKind kind,
        Vector2 worldPosition,
        float playerAimWorldX,
        float runtimePrimarySpeed,
        IClimberAgent targetClimber = null,
        Vector2? launchDirection = null)
    {
        if (!_pools.TryGetValue(kind, out var pool))
        {
            Debug.LogWarning($"[ObstaclePool] No pool registered for {kind}.");
            return null;
        }

        var obstacle = pool.Rent();
        obstacle.BindPool(Release);
        obstacle.Activate(worldPosition, playerAimWorldX, runtimePrimarySpeed, targetClimber, launchDirection);
        return obstacle;
    }

    public void ReleaseAllActive()
    {
        if (_poolRoot == null)
            return;

        var obstacles = _poolRoot.GetComponentsInChildren<ObstacleBase>(true);
        for (int i = 0; i < obstacles.Length; i++)
        {
            var obstacle = obstacles[i];
            if (obstacle != null && obstacle.gameObject.activeInHierarchy)
                Release(obstacle);
        }
    }

    public void Release(ObstacleBase obstacle)
    {
        if (obstacle == null)
            return;

        if (!_pools.TryGetValue(obstacle.Kind, out var pool))
        {
            obstacle.DeactivateLocal();
            return;
        }

        pool.Return(obstacle);
    }
}
