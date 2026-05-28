using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public GameObject[] templates;
    public GameObject endTemplate;

    public bool testSingleTemplate = false;
    public int testTemplateIndex = 0;

    public int mapLength = 10;
    public float segmentGap = 0.5f;
    public float startOffsetY = 1.5f;
  

    private Vector3 currentTopPosition = Vector3.zero;

    void Start()
    {
        currentTopPosition = new Vector3(0, startOffsetY, 0);
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
        GameObject newTemplate = Instantiate(template);

        Transform bottom = newTemplate.transform.Find("Bottom");
        Transform top = newTemplate.transform.Find("Top");

        Vector3 offset = currentTopPosition - bottom.position;
        newTemplate.transform.position += offset;
        currentTopPosition = top.position + Vector3.up * segmentGap;

        currentTopPosition = top.position + Vector3.up * segmentGap;
    }
}