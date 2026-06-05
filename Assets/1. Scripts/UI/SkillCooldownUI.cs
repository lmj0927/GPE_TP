using UnityEngine;
using UnityEngine.UI;

public class SkillCooldownUI : MonoBehaviour
{
    public Image overlay;
    public PlayerObstacleSpawner spawner;
    public ObstacleKind obstacleKind;

    void Update()
    {
        if (overlay == null || spawner == null)
            return;

        float duration = spawner.CooldownDuration(obstacleKind);
        float remain = spawner.RemainingCooldown(obstacleKind);

        if (duration <= 0f)
        {
            overlay.fillAmount = 0f;
            return;
        }

        overlay.fillAmount = remain / duration;
    }
}