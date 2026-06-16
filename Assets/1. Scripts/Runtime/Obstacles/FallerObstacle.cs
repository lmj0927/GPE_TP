using UnityEngine;

/// <summary>
/// Straight drop. Trigger-only (no body collider). Ignores walls and platforms.
/// </summary>
public sealed class FallerObstacle : ObstacleBase
{
    public override ObstacleKind Kind => ObstacleKind.Faller;

    protected override void OnActivated()
    {
        if (Rigidbody != null)
            Rigidbody.linearVelocity = Vector2.zero;
    }

    protected override void OnFixedTick(float deltaTime)
    {
        if (Tuning == null)
            return;

        float speed = RuntimePrimarySpeed > 0f ? RuntimePrimarySpeed : Tuning.Faller.FallSpeed;
        var position = (Vector2)transform.position;
        position.y -= speed * deltaTime;

        if (Rigidbody != null)
        {
            Rigidbody.MovePosition(position);
            Rigidbody.linearVelocity = new Vector2(0f, -speed);
        }
        else
        {
            transform.position = position;
        }
    }
}
