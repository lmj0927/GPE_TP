using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic prefab pool for any <see cref="Component"/> type. Reusable across projects.
/// </summary>
public sealed class ObjectPool<T> where T : Component
{
    private readonly T _prefab;
    private readonly Transform _root;
    private readonly Stack<T> _inactive;
    private readonly bool _reparentOnReturn;

    public Transform Root => _root;
    public int InactiveCount => _inactive.Count;

    public ObjectPool(T prefab, Transform root = null, int prewarmCount = 0, bool reparentOnReturn = true)
    {
        _prefab = prefab;
        _reparentOnReturn = reparentOnReturn;

        if (root != null)
            _root = root;
        else
        {
            var rootObject = new GameObject($"{prefab.name}Pool");
            _root = rootObject.transform;
        }

        _inactive = new Stack<T>(prewarmCount);
        for (int i = 0; i < prewarmCount; i++)
            Return(CreateInstance());
    }

    public T Rent()
    {
        T instance = _inactive.Count > 0 ? _inactive.Pop() : CreateInstance();
        instance.gameObject.SetActive(true);

        if (instance is IPoolable poolable)
            poolable.OnSpawnedFromPool();

        return instance;
    }

    public void Return(T instance)
    {
        if (instance == null)
            return;

        if (instance is IPoolable poolable)
            poolable.OnReturnedToPool();

        instance.gameObject.SetActive(false);

        if (_reparentOnReturn)
            instance.transform.SetParent(_root, false);

        _inactive.Push(instance);
    }

    public void Clear(bool destroyInstances = true)
    {
        if (destroyInstances)
        {
            while (_inactive.Count > 0)
            {
                var instance = _inactive.Pop();
                if (instance != null)
                    Object.Destroy(instance.gameObject);
            }
        }
        else
        {
            _inactive.Clear();
        }
    }

    private T CreateInstance()
    {
        T instance = Object.Instantiate(_prefab, _root);
        instance.gameObject.SetActive(false);
        return instance;
    }
}
