using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : Component
{
    [SerializeField] private bool isDontDestroyOnLoad = false;
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<T>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject();
                    obj.name = typeof(T).Name;
                    _instance = obj.AddComponent<T>();
                }
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            if (isDontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else
        {
            if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        Initialize();
    }

    protected virtual void Initialize()
    {
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}