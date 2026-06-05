using System.Collections;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// Play-mode climber. Matches <see cref="EnemyAgentVer2"/> observations and actions for ONNX inference.
/// Win/lose calls <see cref="GameManager.EndGame"/> instead of <see cref="Agent.EndEpisode"/>.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(ClimberMotor))]
[RequireComponent(typeof(GroundChecker))]
[RequireComponent(typeof(BehaviorParameters))]
public sealed class GameAgent : Agent, IClimberAgent
{
    private const int HorizontalBranch = 0;
    private const int JumpBranch = 1;
    private const int JumpAction = 1;
    private const int ActionLeft = 0;
    private const int ActionIdle = 1;
    private const int ActionRight = 2;
    private const int ObstacleHitDamage = 1;
    private const int LavaDamage = 999;
    private const float JumpTriggerMinUpVelocity = 0.1f;

    /// <summary>Must match <see cref="EnemyAgentVer2.VectorObservationCount"/> and Behavior Parameters.</summary>
    public const int VectorObservationCount = EnemyAgentVer2.VectorObservationCount;

    [SerializeField] private ClimberMovementConfig _config;
    [SerializeField] private Transform _groundCheckOrigin;
    [SerializeField] private Transform _startPoint;
    [SerializeField] private Transform _goalPoint;
    [SerializeField] private RisingLava _risingLava;
    [SerializeField] private Animator _animator;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private float _horizontalDeadZone = 0.15f;

    private Rigidbody2D _rigidbody;
    private ClimberMotor _motor;
    private GroundChecker _groundChecker;
    private BehaviorParameters _behaviorParameters;
    private ClimberStateMachine _stateMachine;
    private ClimberMoveInput _moveInput;
    private bool _isInvincible;
    private bool _gameEnded;
    private int _hitCount;
    private int _health;
    private int _jumpBufferFrames;
    private float _stageSpan = 1f;

    public ClimberStateId CurrentState => _stateMachine.CurrentId;
    public int Health => _health;
    public int MaxHealth => _config != null ? _config.MaxHealth : 1;
    public int HitCount => _hitCount;
    public Vector2 WorldPosition => transform.position;
    public Vector2 WorldVelocity => _rigidbody != null ? _rigidbody.linearVelocity : Vector2.zero;

    private bool UsesDirectKeyboardInput =>
        _behaviorParameters != null &&
        _behaviorParameters.BehaviorType == BehaviorType.HeuristicOnly;

    private static readonly int IsGroundHash = Animator.StringToHash("IsGround");
    private static readonly int JumpTriggerHash = Animator.StringToHash("JumpTrigger");
    private static readonly int StunTriggerHash = Animator.StringToHash("StunTrigger");
    private static readonly int DieTriggerHash = Animator.StringToHash("DieTrigger");

    public override void Initialize()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _motor = GetComponent<ClimberMotor>();
        _groundChecker = GetComponent<GroundChecker>();
        _behaviorParameters = GetComponent<BehaviorParameters>();

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (_groundCheckOrigin == null)
        {
            var child = transform.Find("GroundCheck");
            if (child != null)
                _groundCheckOrigin = child;
        }

        var stageRoot = ResolveStageRoot();
        if (_startPoint == null)
        {
            var start = stageRoot.Find("StartPoint");
            if (start != null)
                _startPoint = start;
        }

        ResolveGoalPoint();

        if (_risingLava == null)
            _risingLava = FindFirstObjectByType<RisingLava>();

        _motor.Configure(_rigidbody, _config);
        _groundChecker.Configure(_groundCheckOrigin, _config);

        var context = new ClimberStateContext(this, _motor, _groundChecker, _config);
        _stateMachine = new ClimberStateMachine(context);

        _groundChecker.Refresh();
        var startState = _groundChecker.IsGrounded ? ClimberStateId.Grounded : ClimberStateId.Airborne;
        _stateMachine.Initialize(startState);
        SyncAnimatorGround(startState == ClimberStateId.Grounded);
        RefreshStageSpan();
    }

    public override void OnEpisodeBegin()
    {
        _gameEnded = false;
        _moveInput = ClimberMoveInput.Zero;
        _isInvincible = false;
        _jumpBufferFrames = 0;
        _health = MaxHealth;

        ResolveGoalPoint();

        if (_startPoint != null)
        {
            transform.position = _startPoint.position;
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
            _rigidbody.simulated = true;
        }

        _risingLava?.ResetToStart();
        RefreshStageSpan();

        _groundChecker.Refresh();
        var startState = _groundChecker.IsGrounded ? ClimberStateId.Grounded : ClimberStateId.Airborne;
        _stateMachine.Initialize(startState);
        ResetAnimatorForEpisode(startState == ClimberStateId.Grounded);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        _groundChecker.Refresh();

        float moveSpeed = _config != null ? _config.MoveSpeed : 7f;
        float jumpSpeed = _config != null ? _config.JumpVelocity : 15f;

        var agentPosition = (Vector2)transform.position;
        var goalPosition = _goalPoint != null ? (Vector2)_goalPoint.position : agentPosition;
        var toGoal = goalPosition - agentPosition;
        float span = _stageSpan;

        sensor.AddObservation(Mathf.Clamp(toGoal.x / span, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(toGoal.y / span, -1f, 1f));

        var velocity = _rigidbody.linearVelocity;
        sensor.AddObservation(Mathf.Clamp(velocity.x / moveSpeed, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(velocity.y / jumpSpeed, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp01(toGoal.magnitude / span));

        float lavaDistance = _risingLava != null ? _risingLava.SurfaceY - agentPosition.y : 0f;
        sensor.AddObservation(Mathf.Clamp(lavaDistance / span, -1f, 1f));

        bool canJump = _groundChecker.IsGrounded && _motor.CanJump;
        sensor.AddObservation(canJump ? 1f : 0f);

        int maxHealth = MaxHealth;
        sensor.AddObservation(maxHealth > 0 ? (float)_health / maxHealth : 0f);
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        _groundChecker.Refresh();

        bool inHitStun = _stateMachine.CurrentId == ClimberStateId.HitStun;
        if (inHitStun)
        {
            actionMask.SetActionEnabled(HorizontalBranch, ActionLeft, false);
            actionMask.SetActionEnabled(HorizontalBranch, ActionRight, false);
        }

        bool canJump = !inHitStun && _groundChecker.IsGrounded && _motor.CanJump;
        actionMask.SetActionEnabled(JumpBranch, JumpAction, canJump);
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
        if (_gameEnded || _stateMachine.CurrentId == ClimberStateId.HitStun)
            return;

        if (MaxStep > 0 && StepCount + 1 >= MaxStep)
        {
            return;
        }

        if (!UsesDirectKeyboardInput)
            _moveInput = ToMoveInput(actions);

        UpdateFacingFromMoveInput(_moveInput.Horizontal);
    }

    private void Update()
    {
        if (_gameEnded)
            return;

        if (UsesDirectKeyboardInput && _stateMachine.CurrentId != ClimberStateId.HitStun)
            _moveInput = ReadKeyboardInput();

        UpdateFacingFromMoveInput(_moveInput.Horizontal);
    }

    private void FixedUpdate()
    {
        if (_gameEnded)
            return;

        _stateMachine.FixedTick(_moveInput);
    }

    public void ChangeState(ClimberStateId stateId)
    {
        var previous = _stateMachine.CurrentId;
        _stateMachine.ChangeState(stateId);
        UpdateAnimatorOnStateChanged(previous, stateId);
    }

    public void NotifyGoalReached()
    {
        if (_gameEnded)
            return;

        EndGameLoss();
    }

    public void NotifyLavaContact() => ApplyDamage(LavaDamage);

    public void ApplyHit()
    {
        if (_gameEnded || _stateMachine.CurrentId == ClimberStateId.HitStun)
            return;

        _hitCount++;
        ApplyDamage(ObstacleHitDamage);
    }

    public void EndInvincibility() => StartCoroutine(EndInvincibilityCoroutine());

    public void ClearJumpBuffer() => _jumpBufferFrames = 0;

    private void ApplyDamage(int amount)
    {
        if (_gameEnded || amount <= 0 || _health <= 0 || _isInvincible)
            return;

        _health -= amount;

        if (_health > 0)
        {
            EnterHitStun();
            return;
        }

        _health = 0;
        Die();
        EndGameWin();
    }

    private void EndGameWin()
    {
        if (_gameEnded)
            return;

        _gameEnded = true;
        _moveInput = ClimberMoveInput.Zero;
        GameManager.Instance.EndGame(true);
    }

    private void EndGameLoss()
    {
        if (_gameEnded)
            return;

        _gameEnded = true;
        _moveInput = ClimberMoveInput.Zero;
        GameManager.Instance.EndGame(false);
    }

    private IEnumerator EndInvincibilityCoroutine()
    {
        yield return new WaitForSeconds(_config.HitStunDuration);
        _isInvincible = false;
    }

    private void Die()
    {
        _rigidbody.simulated = false;
        TriggerAnimator(DieTriggerHash);
    }

    private void EnterHitStun()
    {
        if (_isInvincible)
            return;

        _moveInput = ClimberMoveInput.Zero;
        _jumpBufferFrames = 0;
        _isInvincible = true;
        TriggerAnimator(StunTriggerHash);
        ChangeState(ClimberStateId.HitStun);
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

    private void RefreshStageSpan()
    {
        if (_startPoint != null && _goalPoint != null)
            _stageSpan = Mathf.Abs(_startPoint.position.y - _goalPoint.position.y);
        else
            _stageSpan = 1f;
    }

    private Transform ResolveStageRoot()
    {
        var stage = GameObject.Find("StageRoot");
        return stage != null ? stage.transform : transform.root;
    }

    private void ResolveGoalPoint()
    {
        var goalTrigger = FindFirstObjectByType<ClimberGoalTrigger>();
        _goalPoint = goalTrigger != null ? goalTrigger.transform : null;
    }

    private void ResetAnimatorForEpisode(bool isGrounded)
    {
        if (_animator == null)
            return;

        _animator.Rebind();
        _animator.Update(0f);
        _animator.ResetTrigger(JumpTriggerHash);
        _animator.ResetTrigger(StunTriggerHash);
        _animator.ResetTrigger(DieTriggerHash);
        SyncAnimatorGround(isGrounded);
    }

    private void UpdateAnimatorOnStateChanged(ClimberStateId previous, ClimberStateId next)
    {
        if (_animator == null)
            return;

        SyncAnimatorGround(next == ClimberStateId.Grounded);

        if (previous == ClimberStateId.Grounded &&
            next == ClimberStateId.Airborne &&
            _rigidbody != null &&
            _rigidbody.linearVelocity.y > JumpTriggerMinUpVelocity)
        {
            TriggerAnimator(JumpTriggerHash);
        }
    }

    private void SyncAnimatorGround(bool isGrounded)
    {
        if (_animator == null)
            return;

        _animator.SetBool(IsGroundHash, isGrounded);
    }

    private void TriggerAnimator(int triggerHash)
    {
        if (_animator == null)
            return;

        _animator.SetTrigger(triggerHash);
    }

    private void UpdateFacingFromMoveInput(float horizontal)
    {
        if (_spriteRenderer == null)
            return;

        if (horizontal < -_horizontalDeadZone)
            _spriteRenderer.flipX = true;
        else if (horizontal > _horizontalDeadZone)
            _spriteRenderer.flipX = false;
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Apply Test Hit")]
    private void DebugApplyHit() => ApplyHit();
#endif
}
