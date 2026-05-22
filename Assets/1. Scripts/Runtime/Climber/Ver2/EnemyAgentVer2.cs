using System.Collections;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// Training climber. Shares FSM states via <see cref="IClimberAgent"/>; independent from validation-only agent type.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(ClimberMotor))]
[RequireComponent(typeof(GroundChecker))]
[RequireComponent(typeof(BehaviorParameters))]
public class EnemyAgentVer2 : Agent, IClimberAgent
{
    private const int HorizontalBranch = 0;
    private const int JumpBranch = 1;
    private const int ActionLeft = 0;
    private const int ActionIdle = 1;
    private const int ActionRight = 2;
    private const int ObstacleHitDamage = 1;
    private const int LavaDamage = 999;

    /// <summary>Vector observation count (excludes child ray sensors). Match Behavior Parameters.</summary>
    public const int VectorObservationCount = 7;

    [SerializeField] private ClimberMovementConfig _config;
    [SerializeField] private ClimberVer2RewardWeightConfig _rewardWeights;
    [SerializeField] private Transform _groundCheckOrigin;
    [SerializeField] private Transform _startPoint;
    [SerializeField] private Transform _goalPoint;
    [SerializeField] private RisingLava _risingLava;
    [SerializeField] private HeuristicSpawnerBot _heuristicSpawner;

    [SerializeField] private float _horizontalDeadZone = 0.15f;
    [SerializeField] private int _maxHealth = 3;

    private Rigidbody2D _rigidbody;
    private ClimberMotor _motor;
    private GroundChecker _groundChecker;
    private BehaviorParameters _behaviorParameters;
    private ClimberStateMachine _stateMachine;
    private ClimberMoveInput _moveInput;
    private bool _isInvincible;
    private int _health;
    private int _jumpBufferFrames;

    private float _stageSpan = 1f;
    private float _bestY;
    private float _lastLandingY;
    private float _bestDistToGoal;
    private int _lastLandingPlatformInstanceId;
    private int _platformLandingsThisEpisode;
    private bool _skipNextEpisodeStatsReport = true;
    private int _stallPlatformInstanceId;
    private float _platformStallElapsed;
    private float _lastPlatformStallSampleTime;

    public ClimberStateId CurrentState => _stateMachine.CurrentId;

    public int Health => _health;
    public int MaxHealth => _maxHealth;

    public Vector2 WorldPosition => transform.position;

    public Vector2 WorldVelocity => _rigidbody != null ? _rigidbody.linearVelocity : Vector2.zero;

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
        RefreshStageSpan();
    }

    public override void OnEpisodeBegin()
    {
        ReportEpisodeStats();
        _platformLandingsThisEpisode = 0;

        _moveInput = ClimberMoveInput.Zero;
        _isInvincible = false;
        _jumpBufferFrames = 0;
        _health = _maxHealth;

        if (_startPoint != null)
        {
            transform.position = _startPoint.position;
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
            _rigidbody.simulated = true;
        }

        _risingLava?.ResetToStart();
        RefreshStageSpan();

        _bestY = transform.position.y;
        _lastLandingY = _bestY;
        _bestDistToGoal = GetDistanceToGoal();
        _lastLandingPlatformInstanceId = 0;
        ResetPlatformStallTracking();

        _groundChecker.Refresh();
        var startState = _groundChecker.IsGrounded ? ClimberStateId.Grounded : ClimberStateId.Airborne;
        _stateMachine.Initialize(startState);

        _heuristicSpawner?.ResetForEpisode();
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
    }

    private void FixedUpdate() => _stateMachine.FixedTick(_moveInput);

    private void ApplyDecisionRewards()
    {
        if (_rewardWeights == null)
            return;

        _groundChecker.Refresh();
        ApplyPlatformStallPenalty();
    }

    private void ResetPlatformStallTracking()
    {
        _stallPlatformInstanceId = 0;
        _platformStallElapsed = 0f;
        _lastPlatformStallSampleTime = Time.time;
    }

    private void ApplyPlatformStallPenalty()
    {
        if (_rewardWeights.PlatformStallPenaltyPerDecision == 0f)
            return;

        if (!_groundChecker.IsGrounded)
            return;

        var platform = _groundChecker.CurrentPlatform;
        if (platform == null)
            return;

        int platformId = platform.GetInstanceID();
        float now = Time.time;

        if (platformId != _stallPlatformInstanceId)
        {
            _stallPlatformInstanceId = platformId;
            _platformStallElapsed = 0f;
            _lastPlatformStallSampleTime = now;
            return;
        }

        _platformStallElapsed += now - _lastPlatformStallSampleTime;
        _lastPlatformStallSampleTime = now;

        if (_platformStallElapsed < _rewardWeights.PlatformStallTimeSeconds)
            return;

        AddReward(_rewardWeights.PlatformStallPenaltyPerDecision);
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

        _groundChecker.Refresh();
        int landingPlatformId = _groundChecker.CurrentPlatform != null
            ? _groundChecker.CurrentPlatform.GetInstanceID()
            : 0;

        float landingY = transform.position.y;
        float minDelta = _rewardWeights.MinLandingHeightDelta;

        if (landingY > _bestY + minDelta)
        {
            _bestY = landingY;
            _lastLandingY = landingY;
            AddReward(_rewardWeights.PlatformLandingReward);
            _platformLandingsThisEpisode++;
            RecordStat("Environment/PlatformLanding", 1f, StatAggregationMethod.Sum);
        }
        else if (landingY < _lastLandingY - minDelta && _rewardWeights.PlatformLandingDownPenalty != 0f)
        {
            _lastLandingY = landingY;
            AddReward(_rewardWeights.PlatformLandingDownPenalty);
            RecordStat("Environment/PlatformLandingDown", 1f, StatAggregationMethod.Sum);
        }
        else if (landingY > _lastLandingY + minDelta && _rewardWeights.PlatformLandingRecoveryReward != 0f)
        {
            _lastLandingY = landingY;
            AddReward(_rewardWeights.PlatformLandingRecoveryReward);
        }
        else
        {
            _lastLandingY = landingY;
        }

        TryRewardGoalDistanceProgress(minDelta, landingPlatformId);

        if (landingPlatformId != 0)
            _lastLandingPlatformInstanceId = landingPlatformId;
    }

    private float GetDistanceToGoal()
    {
        if (_goalPoint == null)
            return float.MaxValue;

        return Vector2.Distance((Vector2)transform.position, (Vector2)_goalPoint.position);
    }

    private void TryRewardGoalDistanceProgress(float minDelta, int landingPlatformId)
    {
        if (_rewardWeights.GoalDistanceProgressReward == 0f || _goalPoint == null)
            return;

        if (landingPlatformId != 0 && landingPlatformId == _lastLandingPlatformInstanceId)
            return;

        float dist = GetDistanceToGoal();
        if (dist >= _bestDistToGoal - minDelta)
            return;

        _bestDistToGoal = dist;
        AddReward(_rewardWeights.GoalDistanceProgressReward);
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

    public void NotifyGoalReached()
    {
        if (_rewardWeights != null)
            AddReward(_rewardWeights.GoalReachedReward);
        EndEpisode();
    }

    public void NotifyLavaContact()
    {
        ApplyDamage(LavaDamage);
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
        if (_stateMachine.CurrentId == ClimberStateId.HitStun)
            return;

        ApplyDamage(ObstacleHitDamage);
    }

    public void EndInvincibility()
    {
        StartCoroutine(EndInvincibilityCoroutine());
    }

    IEnumerator EndInvincibilityCoroutine()
    {
        yield return new WaitForSeconds(_config.HitStunDuration);
        _isInvincible = false;
    }

    public void Die()
    {
        //enabled = false;
        _rigidbody.simulated = false;
    }

    private void ApplyDamage(int amount)
    {
        if (amount <= 0 || _health <= 0 || _isInvincible)
            return;

        _health -= amount;

        if (amount == ObstacleHitDamage)
            ApplyHitPenalty();

        if (_health > 0)
        {
            EnterHitStun();
            return;
        }

        _health = 0;
        ApplyDeadPenalty();
        Die();
        EndEpisode();
    }

    private void ApplyHitPenalty()
    {
        if (_rewardWeights != null)
            AddReward(_rewardWeights.HitPenalty);
    }

    private void ApplyDeadPenalty()
    {
        if (_rewardWeights != null)
            AddReward(_rewardWeights.DeadPenalty);
    }

    private void EnterHitStun()
    {
        if (_isInvincible)
            return;

        _moveInput = ClimberMoveInput.Zero;
        _jumpBufferFrames = 0;
        _isInvincible = true;
        ChangeState(ClimberStateId.HitStun);
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Apply Test Hit")]
    private void DebugApplyHit() => ApplyHit();
#endif
}
