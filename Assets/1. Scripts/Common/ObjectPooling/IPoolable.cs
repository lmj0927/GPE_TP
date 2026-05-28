/// <summary>
/// Optional lifecycle hooks when using <see cref="ObjectPool{T}"/>.
/// </summary>
public interface IPoolable
{
    void OnSpawnedFromPool();
    void OnReturnedToPool();
}
