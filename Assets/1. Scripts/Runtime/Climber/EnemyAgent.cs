using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(ClimberMotor))]
[RequireComponent(typeof(GroundChecker))]
[RequireComponent(typeof(BehaviorParameters))]
public class EnemyAgent : Agent
{
    private const int HorizontalBranch = 0;
    private const int JumpBranch = 1;
    private const int ActionLeft = 0;
    private const int ActionIdle = 1;
    private const int ActionRight = 2;

    [SerializeField] private ClimberMovementConfig _config;
    [SerializeField] private ClimberRewardWeightConfig _rewardWeights;
    [SerializeField] private Transform _groundCheckOrigin;
    [SerializeField] private Transform _startPoint;
    [SerializeField] private Transform _goalPoint;
    [SerializeField] private RisingLava _risingLava;

    [SerializeField] private float _horizontalDeadZone = 0.15f;

    private Rigidbody2D _rigidbody;
    private ClimberMotor _motor;
    private GroundChecker _groundChecker;
    private BehaviorParameters _behaviorParameters;
    private ClimberStateMachine _stateMachine;
    private ClimberMoveInput _moveInput;
    private bool _isInvincible;
    private int _jumpBufferFrames;

    private Vector2 _observationOrigin;
    private float _bestY;
    private float _previousGoalDistanceY;
    private int _platformLandingsThisEpisode;
    private bool _skipNextEpisodeStatsReport = true;

    public ClimberStateId CurrentState => _stateMachine.CurrentId;

    private bool UsesDirectKeyboardInput =>
        _behaviorParameters != null &&
        _behaviorParameters.BehaviorType == BehaviorType.HeuristicOnly;

    public override void Initialize()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _motor = GetComponent<ClimberMotor>();
        _groundChecker = GetComponent<GroundChecker>();
        _behaviorParameters = GetComponent<BehaviorParameters>();

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

        if (_goalPoint == null)
        {
            var goal = stageRoot.Find("Climb_Goal");
            if (goal != null)
                _goalPoint = goal;
        }

        if (_risingLava == null)
            _risingLava = FindFirstObjectByType<RisingLava>();

        _motor.Configure(_rigidbody, _config);
        _groundChecker.Configure(_groundCheckOrigin, _config);

        var context = new ClimberStateContext(this, _motor, _groundChecker, _config);
        _stateMachine = new ClimberStateMachine(context);

        _groundChecker.Refresh();
        var startState = _groundChecker.IsGrounded ? ClimberStateId.Grounded : ClimberStateId.Airborne;
        _stateMachine.Initialize(startState);
    }

    public override void OnEpisodeBegin()
    {
        ReportEpisodeStats();
        _platformLandingsThisEpisode = 0;

        _moveInput = ClimberMoveInput.Zero;
        _isInvincible = false;
        _jumpBufferFrames = 0;

        if (_startPoint != null)
        {
            transform.position = _startPoint.position;
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
            _rigidbody.simulated = true;
        }

        _risingLava?.ResetToStart();

        _observationOrigin = _startPoint != null ? (Vector2)_startPoint.position : (Vector2)transform.position;
        _bestY = transform.position.y;
        _previousGoalDistanceY = ComputeGoalDistanceY();

        _groundChecker.Refresh();
        var startState = _groundChecker.IsGrounded ? ClimberStateId.Grounded : ClimberStateId.Airborne;
        _stateMachine.Initialize(startState);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        _groundChecker.Refresh();

        float moveSpeed = _config != null ? _config.MoveSpeed : 7f;
        float jumpSpeed = _config != null ? _config.JumpVelocity : 15f;

        var agentPosition = (Vector2)transform.position;
        var goalPosition = _goalPoint != null ? (Vector2)_goalPoint.position : agentPosition;
        var goalRelative = goalPosition - _observationOrigin;
        var agentRelative = agentPosition - _observationOrigin;

        sensor.AddObservation(goalRelative.x);
        sensor.AddObservation(goalRelative.y);
        sensor.AddObservation(agentRelative.x);
        sensor.AddObservation(agentRelative.y);

        var velocity = _rigidbody.linearVelocity;
        sensor.AddObservation(Mathf.Clamp(velocity.x / moveSpeed, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(velocity.y / jumpSpeed, -1f, 1f));

        sensor.AddObservation(goalPosition.x - agentPosition.x);

        float lavaDistance = _risingLava != null ? _risingLava.SurfaceY - agentPosition.y : 0f;
        sensor.AddObservation(lavaDistance);

        bool canJump = _groundChecker.IsGrounded && _motor.CanJump;
        sensor.AddObservation(canJump ? 1f : 0f);
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

        ApplyDecisionRewards();

        if (MaxStep > 0 && StepCount + 1 >= MaxStep)
        {
            EndEpisode();
            return;
        }

        if (!UsesDirectKeyboardInput)
            _moveInput = ToMoveInput(actions);
    }

    private void Update()
    {
        if (UsesDirectKeyboardInput && _stateMachine.CurrentId != ClimberStateId.HitStun)
            _moveInput = ReadKeyboardInput();

        _stateMachine.Tick(_moveInput);
    }

    private void FixedUpdate() => _stateMachine.FixedTick(_moveInput);

    private void ApplyDecisionRewards()
    {
        if (_rewardWeights == null)
            return;

        _groundChecker.Refresh();
        ApplyGoalDistanceYShaping();
        ApplyPlatformIdlePenalty();
    }

    private void ApplyGoalDistanceYShaping()
    {
        float goalDistanceY = ComputeGoalDistanceY();
        float threshold = _rewardWeights.GoalDistanceYThreshold;

        if (goalDistanceY < _previousGoalDistanceY - threshold)
            AddReward(_rewardWeights.GoalApproachRewardPerDecision);
        else if (goalDistanceY > _previousGoalDistanceY + threshold)
            AddReward(_rewardWeights.GoalRecedePenaltyPerDecision);

        _previousGoalDistanceY = goalDistanceY;
    }

    private void ApplyPlatformIdlePenalty()
    {
        if (_rewardWeights.PlatformIdlePenaltyPerDecision == 0f)
            return;

        float y = transform.position.y;
        float minDelta = _rewardWeights.MinLandingHeightDelta;
        if (!_groundChecker.IsGrounded || Mathf.Abs(y - _bestY) > minDelta)
            return;

        AddReward(_rewardWeights.PlatformIdlePenaltyPerDecision);
    }

    private float ComputeGoalDistanceY()
    {
        if (_goalPoint == null)
            return 0f;

        return _goalPoint.position.y - transform.position.y;
    }

    public void ChangeState(ClimberStateId stateId)
    {
        var previous = _stateMachine.CurrentId;
        _stateMachine.ChangeState(stateId);

        if (stateId == ClimberStateId.Grounded && previous == ClimberStateId.Airborne)
            TryRewardNewPlatformLanding();
    }

    private void TryRewardNewPlatformLanding()
    {
        if (_rewardWeights == null)
            return;

        float landingY = transform.position.y;
        float minDelta = _rewardWeights.MinLandingHeightDelta;

        if (landingY > _bestY + minDelta)
        {
            _bestY = landingY;
            AddReward(_rewardWeights.PlatformLandingReward);
            _platformLandingsThisEpisode++;
            RecordStat("Environment/PlatformLanding", 1f, StatAggregationMethod.Sum);
            return;
        }

        if (landingY < _bestY - minDelta && _rewardWeights.PlatformLandingDownPenalty != 0f)
        {
            AddReward(_rewardWeights.PlatformLandingDownPenalty);
            RecordStat("Environment/PlatformLandingDown", 1f, StatAggregationMethod.Sum);
        }
    }

    private void ReportEpisodeStats()
    {
        if (_skipNextEpisodeStatsReport)
        {
            _skipNextEpisodeStatsReport = false;
            return;
        }

        RecordStat(
            "Environment/PlatformLandingsPerEpisode",
            _platformLandingsThisEpisode,
            StatAggregationMethod.MostRecent);
    }

    private static void RecordStat(string key, float value, StatAggregationMethod aggregation)
    {
        if (Academy.Instance == null || !Academy.Instance.IsCommunicatorOn)
            return;

        Academy.Instance.StatsRecorder.Add(key, value, aggregation);
    }

    private Transform ResolveStageRoot()
    {
        var stage = GameObject.Find("StageRoot");
        return stage != null ? stage.transform : transform.root;
    }

    public void NotifyGoalReached()
    {
        if (_rewardWeights != null)
            AddReward(_rewardWeights.GoalReachedReward);
        EndEpisode();
    }

    public void NotifyLavaContact()
    {
        if (_rewardWeights != null)
            AddReward(_rewardWeights.LavaContactPenalty);
        EndEpisode();
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
