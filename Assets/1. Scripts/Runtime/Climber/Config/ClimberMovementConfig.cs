using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "ClimberMovementConfig", menuName = "GPE/Climber/Movement Config")]
public class ClimberMovementConfig : ScriptableObject
{
    [SerializeField] private float _moveSpeed = 7f;
    [SerializeField] private float _groundAcceleration = 60f;
    [SerializeField] private float _airAcceleration = 40f;
    [SerializeField] private float _jumpVelocity = 15f;
    [SerializeField] private float _gravityScale = 3f;
    [SerializeField] private float _hitStunDuration = 1f;
    [SerializeField, Min(1)] private int _maxHealth = 3;
    [FormerlySerializedAs("_groundRayDistance")]
    [SerializeField] private float _groundCheckBoxHeight = 0.2f;
    [FormerlySerializedAs("_groundCheckRadius")]
    [SerializeField] private float _groundCheckBoxLength = 0.25f;
    [SerializeField] private int _jumpBufferFrames = 8;
    [SerializeField] private LayerMask _groundLayers;

    public float MoveSpeed => _moveSpeed;
    public float GroundAcceleration => _groundAcceleration;
    public float AirAcceleration => _airAcceleration;
    public float JumpVelocity => _jumpVelocity;
    public float GravityScale => _gravityScale;
    public float HitStunDuration => _hitStunDuration;
    public int MaxHealth => _maxHealth;
    public float GroundCheckBoxHeight => _groundCheckBoxHeight;
    public float GroundCheckBoxLength => _groundCheckBoxLength;
    public int JumpBufferFrames => _jumpBufferFrames;
    public LayerMask GroundLayers => _groundLayers;

    public float GravityY => Physics2D.gravity.y * _gravityScale;
}
