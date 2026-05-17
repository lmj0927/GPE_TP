using UnityEngine;

[CreateAssetMenu(fileName = "ClimberMovementConfig", menuName = "GPE/Climber/Movement Config")]
public class ClimberMovementConfig : ScriptableObject
{
    [SerializeField] private float _moveSpeed = 7f;
    [SerializeField] private float _groundAcceleration = 60f;
    [SerializeField] private float _airAcceleration = 40f;
    [SerializeField] private float _jumpVelocity = 15f;
    [SerializeField] private float _gravityScale = 3f;
    [SerializeField] private float _hitStunDuration = 1f;
    [SerializeField] private float _groundRayDistance = 0.2f;
    [SerializeField] private int _jumpBufferFrames = 8;
    [SerializeField] private LayerMask _groundLayers;

    public float MoveSpeed => _moveSpeed;
    public float GroundAcceleration => _groundAcceleration;
    public float AirAcceleration => _airAcceleration;
    public float JumpVelocity => _jumpVelocity;
    public float GravityScale => _gravityScale;
    public float HitStunDuration => _hitStunDuration;
    public float GroundRayDistance => _groundRayDistance;
    public int JumpBufferFrames => _jumpBufferFrames;
    public LayerMask GroundLayers => _groundLayers;

    public float GravityY => Physics2D.gravity.y * _gravityScale;
}
