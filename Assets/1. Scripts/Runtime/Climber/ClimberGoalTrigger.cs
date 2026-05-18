using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class ClimberGoalTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        var agent = other.GetComponent<EnemyAgent>() ?? other.GetComponentInParent<EnemyAgent>();
        agent?.NotifyGoalReached();
    }
}
