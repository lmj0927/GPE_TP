using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(ClimberMotor))]
[RequireComponent(typeof(GroundChecker))]
public class EnemyAgent : Agent
{
    private const int HorizontalBranch = 0;
    private const int JumpBranch = 1;
    private const int ActionLeft = 0;
    private const int ActionIdle = 1;
    private const int ActionRight = 2;

    [SerializeField] private ClimberMovementConfig _config;
    [SerializeField] private Transform _groundCheckOrigin;
    //[SerializeField] private AStarPlanner _planner;
    [SerializeField] private float _horizontalDeadZone = 0.15f;
    [SerializeField] private float _replanInterval = 1.5f;
    [SerializeField] private bool _pollKeyboardInput = true;

    private Rigidbody2D _rigidbody;
    private ClimberMotor _motor;
    private GroundChecker _groundChecker;
    private ClimberStateMachine _stateMachine;
    private ClimberMoveInput _moveInput;
    private bool _isInvincible;
    private float _replanTimer;
    private int _jumpBufferFrames;

    public ClimberStateId CurrentState => _stateMachine.CurrentId;

    public override void Initialize()
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

        var context = new ClimberStateContext(this, _motor, _groundChecker, _config);
        _stateMachine = new ClimberStateMachine(context);

        _groundChecker.Refresh();
        var start = _groundChecker.IsGrounded ? ClimberStateId.Grounded : ClimberStateId.Airborne;
        _stateMachine.Initialize(start);
    }

    public override void OnEpisodeBegin()
    {
        _moveInput = ClimberMoveInput.Zero;
        _replanTimer = 0f;
        _isInvincible = false;
        _jumpBufferFrames = 0;

        _groundChecker.Refresh();
        var start = _groundChecker.IsGrounded ? ClimberStateId.Grounded : ClimberStateId.Airborne;
        _stateMachine.Initialize(start);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;
        discrete[HorizontalBranch] = ActionIdle;
        discrete[JumpBranch] = 0;

        if (_stateMachine.CurrentId == ClimberStateId.HitStun)
            return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        discrete[HorizontalBranch] = ToHorizontalAction(
            horizontal < -_horizontalDeadZone ? -1f : horizontal > _horizontalDeadZone ? 1f : 0f);

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump"))
            discrete[JumpBranch] = 1;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (_stateMachine.CurrentId == ClimberStateId.HitStun)
            return;

        if (_pollKeyboardInput)
            return;

        _moveInput = ToMoveInput(actions);
    }

    private void Update()
    {
        if (_pollKeyboardInput && _stateMachine.CurrentId != ClimberStateId.HitStun)
            _moveInput = ReadKeyboardInput();

        _stateMachine.Tick(_moveInput);
    }

    private void FixedUpdate() => _stateMachine.FixedTick(_moveInput);

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

    public void ClearJumpBuffer() => _jumpBufferFrames = 0;

    private static int ToHorizontalAction(float horizontal)
    {
        if (horizontal < 0f)
            return ActionLeft;
        if (horizontal > 0f)
            return ActionRight;
        return ActionIdle;
    }

    private static ClimberMoveInput ToMoveInput(ActionBuffers actions)
    {
        int horizontal = actions.DiscreteActions[HorizontalBranch];
        bool jump = actions.DiscreteActions[JumpBranch] == 1;

        float move = horizontal switch
        {
            ActionLeft => -1f,
            ActionRight => 1f,
            _ => 0f
        };

        return new ClimberMoveInput(move, jump);
    }

    public void ChangeState(ClimberStateId stateId) => _stateMachine.ChangeState(stateId);

    public void ApplyHit()
    {
        if (_isInvincible)
            return;

        _moveInput = ClimberMoveInput.Zero;
        _jumpBufferFrames = 0;
        _isInvincible = true;
        ChangeState(ClimberStateId.HitStun);
    }

    public void EndInvincibility() => _isInvincible = false;

    public void Die()
    {
        enabled = false;
        _rigidbody.simulated = false;
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Apply Test Hit")]
    private void DebugApplyHit() => ApplyHit();
#endif
}
