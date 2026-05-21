using UnityEngine;

/// <summary>
/// ML-Agents 없이 맵 오르기만 검증. <see cref="EnemyAgent"/> HeuristicOnly 키보드 입력 경로와 동일.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(ClimberMotor))]
[RequireComponent(typeof(GroundChecker))]
public sealed class TestEnemy : MonoBehaviour
{
    [SerializeField] private ClimberMovementConfig _config;
    [SerializeField] private Transform _groundCheckOrigin;
    [SerializeField] private float _horizontalDeadZone = 0.15f;

    private Rigidbody2D _rigidbody;
    private ClimberMotor _motor;
    private GroundChecker _groundChecker;
    private ClimberStateId _state;
    private ClimberMoveInput _moveInput;
    private int _jumpBufferFrames;

    public ClimberStateId CurrentState => _state;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _motor = GetComponent<ClimberMotor>();
        _groundChecker = GetComponent<GroundChecker>();

        if (_groundCheckOrigin == null)
        {
            var child = transform.Find("GroundCheck");
            if (child != null)
                _groundCheckOrigin = child;
        }

        _motor.Configure(_rigidbody, _config);
        _groundChecker.Configure(_groundCheckOrigin, _config);

        _groundChecker.Refresh();
        _state = _groundChecker.IsGrounded ? ClimberStateId.Grounded : ClimberStateId.Airborne;
        if (_state == ClimberStateId.Grounded)
            _motor.ResetJumpOnLanding();
    }

    private void Update()
    {
        _moveInput = ReadKeyboardInput();
    }

    private void FixedUpdate()
    {
        if (_state == ClimberStateId.Grounded)
            FixedTickGrounded(_moveInput);
        else
            FixedTickAirborne(_moveInput);
    }

    private ClimberMoveInput ReadKeyboardInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float move = 0f;
        if (horizontal < -_horizontalDeadZone)
            move = -1f;
        else if (horizontal > _horizontalDeadZone)
            move = 1f;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump"))
            _jumpBufferFrames = _config == null || _config.JumpBufferFrames <= 0
                ? 8
                : _config.JumpBufferFrames;

        bool jump = _jumpBufferFrames > 0;
        if (_jumpBufferFrames > 0)
            _jumpBufferFrames--;

        return new ClimberMoveInput(move, jump);
    }

    private void FixedTickGrounded(ClimberMoveInput input)
    {
        _groundChecker.Refresh();
        float deltaTime = Time.fixedDeltaTime;

        if (!_groundChecker.IsGrounded)
        {
            ChangeState(ClimberStateId.Airborne);
            _motor.ApplyHorizontal(input.Horizontal, grounded: false, deltaTime);
            return;
        }

        if (input.Jump && _motor.CanJump && _motor.TryJump())
        {
            ClearJumpBuffer();
            ChangeState(ClimberStateId.Airborne);
            _motor.ApplyHorizontal(input.Horizontal, grounded: false, deltaTime);
            return;
        }

        _motor.ApplyHorizontal(input.Horizontal, grounded: true, deltaTime);
    }

    private void FixedTickAirborne(ClimberMoveInput input)
    {
        _groundChecker.Refresh();

        if (_groundChecker.IsGrounded)
            ChangeState(ClimberStateId.Grounded);

        _motor.ApplyHorizontal(input.Horizontal, grounded: false, Time.fixedDeltaTime);
    }

    private void ChangeState(ClimberStateId nextId)
    {
        if (_state == nextId)
            return;

        if (nextId == ClimberStateId.Grounded)
            _motor.ResetJumpOnLanding();

        _state = nextId;
    }

    private void ClearJumpBuffer() => _jumpBufferFrames = 0;
}
