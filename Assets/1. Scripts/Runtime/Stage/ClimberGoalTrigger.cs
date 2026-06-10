using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class ClimberGoalTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<IClimberAgent>(out var climber))
            climber.NotifyGoalReached();
    }
}
