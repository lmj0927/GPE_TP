using UnityEngine;

[CreateAssetMenu(fileName = "SpawnerPatternConfig", menuName = "GPE/Obstacles/Spawner Pattern Config")]
public sealed class SpawnerPatternConfig : ScriptableObject
{
    [SerializeField, Min(0f)] private float _spawnIntervalSeconds = 1f;
    [SerializeField] private float _aimLeadSeconds = 0.4f;
    [SerializeField] private float _spawnHeightOffset = 30f;
    [SerializeField] private float _bouncerMinDownTiltDegrees = 10f;
    [SerializeField] private float _bouncerMaxDownTiltDegrees = 45f;

    public float SpawnIntervalSeconds => _spawnIntervalSeconds;
    public float AimLeadSeconds => _aimLeadSeconds;
    public float SpawnHeightOffset => _spawnHeightOffset;
    public float BouncerMinDownTiltDegrees => _bouncerMinDownTiltDegrees;
    public float BouncerMaxDownTiltDegrees => _bouncerMaxDownTiltDegrees;
}
