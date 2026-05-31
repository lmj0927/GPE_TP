using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class ClimberMotor : MonoBehaviour
{
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private ClimberMovementConfig _config;

    private bool _jumpConsumed;

    public bool CanJump => !_jumpConsumed;

    public void Configure(Rigidbody2D rigidbody, ClimberMovementConfig config)
    {
        _rigidbody = rigidbody;
        _config = config;
        _rigidbody.gravityScale = config.GravityScale;
    }

    public void ApplyHorizontal(float direction, bool grounded, float deltaTime)
    {
        if (_rigidbody == null || _config == null)
            return;

        float targetX = Mathf.Clamp(direction, -1f, 1f) * _config.MoveSpeed;
        var velocity = _rigidbody.linearVelocity;
        velocity.x = targetX;
        _rigidbody.linearVelocity = velocity;
    }

    public bool TryJump()
    {
        if (_rigidbody == null || _config == null || _jumpConsumed)
            return false;

        var velocity = _rigidbody.linearVelocity;
        velocity.y = _config.JumpVelocity;
        _rigidbody.linearVelocity = velocity;
        _jumpConsumed = true;
        return true;
    }

    public void ResetJumpOnLanding()
    {
        _jumpConsumed = false;
    }

    public void Halt()
    {
        if (_rigidbody == null)
            return;

        var velocity = _rigidbody.linearVelocity;
        velocity.x = 0f;
        _rigidbody.linearVelocity = velocity;
        _rigidbody.angularVelocity = 0f;
    }

    public void SetStunned(bool stunned)
    {
        if (_rigidbody == null || _config == null)
            return;

        
        if (stunned)
            Halt();
    }
}
