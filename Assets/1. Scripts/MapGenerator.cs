using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public GameObject[] templates;
    public GameObject endTemplate;

    public bool testSingleTemplate = false;
    public int testTemplateIndex = 0;

    public int mapLength = 10;
    [SerializeField] private int offsetY = 30;

    private Vector3 currentTopPosition = Vector3.zero;

    void Start()
    {
        currentTopPosition += Vector3.up * offsetY;
        GenerateMap();
    }

    void GenerateMap()
    {
        if (testSingleTemplate)
        {
            SpawnTemplate(templates[testTemplateIndex]);
            return;
        }

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

    void SpawnTemplate(GameObject template)
    {
        GameObject newTemplate = Instantiate(template, currentTopPosition, Quaternion.identity);

        currentTopPosition += Vector3.up * offsetY;
    }
}