using UnityEngine;
using UnityEngine.UI;

public class HeightSliderUI : MonoBehaviour
{
    public Slider heightSlider;
    public Transform enemy;
    public Transform startPoint;

    private Transform goalPoint;

    void Update()
    {
        if (goalPoint == null)
        {
            GameObject goalObj = GameObject.Find("Goal");

            if (goalObj != null)
            {
                goalPoint = goalObj.transform;
            }
        }

        if (heightSlider == null || enemy == null || startPoint == null || goalPoint == null)
            return;

        float progress = Mathf.InverseLerp(
            startPoint.position.y,
            goalPoint.position.y,
            enemy.position.y
        );

        heightSlider.value = Mathf.Clamp01(progress);
    }
}