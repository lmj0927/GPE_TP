using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public GameObject[] templates;
    public GameObject endTemplate;

    public bool testSingleTemplate = false;
    public int testTemplateIndex = 0;

    public int mapLength = 10;
    [SerializeField] private int offsetY = 30;

    private Transform _spawnRoot;
    private Vector3 currentTopPosition;
    [SerializeField] private bool generateOnAwake = false;

    private void Awake()
    {
        EnsureSpawnRoot();
        if (generateOnAwake)
            RegenerateMap();
    }

    public void RegenerateMap()
    {
        ClearSpawned();
        currentTopPosition = transform.position + Vector3.up * offsetY;
        GenerateMap();
    }

    private void GenerateMap()
    {
        if (templates == null || templates.Length == 0)
            return;

        if (testSingleTemplate)
        {
            SpawnTemplate(templates[testTemplateIndex]);
            return;
        }

        if (endTemplate == null)
            return;

        int lastIndex = -1;

        for (int i = 0; i < mapLength; i++)
        {
            int randomIndex;

            do
            {
                randomIndex = Random.Range(0, templates.Length);
            }
            while (randomIndex == lastIndex);

            lastIndex = randomIndex;

            SpawnTemplate(templates[randomIndex]);
        }

        SpawnTemplate(endTemplate);
    }

    private void SpawnTemplate(GameObject template)
    {
        if (template == null)
            return;

        Instantiate(template, currentTopPosition, Quaternion.identity, _spawnRoot);
        currentTopPosition += Vector3.up * offsetY;
    }

    private void EnsureSpawnRoot()
    {
        if (_spawnRoot != null)
            return;

        var rootObject = new GameObject("GeneratedMap");
        _spawnRoot = rootObject.transform;
        _spawnRoot.SetParent(transform);
        _spawnRoot.localPosition = Vector3.zero;
    }

    private void ClearSpawned()
    {
        if (_spawnRoot == null)
            return;

        for (int i = _spawnRoot.childCount - 1; i >= 0; i--)
        {
            var chunk = _spawnRoot.GetChild(i).gameObject;
            chunk.SetActive(false);
            Destroy(chunk);
        }
    }
}
