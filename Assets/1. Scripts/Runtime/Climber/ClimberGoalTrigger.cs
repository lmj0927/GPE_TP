using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class ClimberGoalTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<MonoBehaviour>() is IClimberAgent climber)
            climber.NotifyGoalReached();
    }
}
