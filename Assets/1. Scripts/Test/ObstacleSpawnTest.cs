using UnityEngine;

/// <summary>
/// Manual obstacle pool test. Keys 1/2/3 spawn at mouse X and spawn height.
/// </summary>
public sealed class ObstacleSpawnTest : MonoBehaviour
{
    [SerializeField] private ObstaclePool _obstaclePool;
    [SerializeField] private float _spawnHeight = 12f;
    [SerializeField] private float _debugPlayerAimX;

    private Camera _camera;

    private void Awake()
    {
        _camera = Camera.main;
    }

    private void Update()
    {
        if (_obstaclePool == null || _camera == null)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            Spawn(ObstacleKind.Faller);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            Spawn(ObstacleKind.Bouncer);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            Spawn(ObstacleKind.Roller);
    }

    private void Spawn(ObstacleKind kind)
    {
        var world = _camera.ScreenToWorldPoint(Input.mousePosition);
        world.z = 0f;
        world.y = _spawnHeight;

        float aimX = Mathf.Approximately(_debugPlayerAimX, 0f) ? world.x : _debugPlayerAimX;
        _obstaclePool.Rent(kind, world, aimX);
    }
}
