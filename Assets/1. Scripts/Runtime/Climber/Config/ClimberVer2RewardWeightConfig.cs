using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "ClimberVer2RewardWeightConfig", menuName = "GPE/Climber/Ver2/Reward Weight Config")]
public sealed class ClimberVer2RewardWeightConfig : ScriptableObject
{
    [SerializeField] private float _platformLandingReward = 1f;
    [SerializeField] private float _platformLandingRecoveryReward = 0.3f;
    [SerializeField] private float _platformLandingDownPenalty = -0.5f;
    [SerializeField] private float _goalReachedReward = 10f;
    [SerializeField] private float _goalDistanceProgressReward = 0.2f;
    [FormerlySerializedAs("_lavaContactPenalty")]
    [SerializeField] private float _deadPenalty = -10f;
    [SerializeField] private float _hitPenalty = -1f;
    [SerializeField] private float _survivalRewardPerDecision = 0.0005f;
    [SerializeField] private float _platformStallPenaltyPerDecision = -0.001f;
    [SerializeField] private float _platformStallTimeSeconds = 3f;
    [SerializeField] private float _minLandingHeightDelta = 0.05f;

    public float PlatformLandingReward => _platformLandingReward;
    public float PlatformLandingRecoveryReward => _platformLandingRecoveryReward;
    public float PlatformLandingDownPenalty => _platformLandingDownPenalty;
    public float GoalReachedReward => _goalReachedReward;
    public float GoalDistanceProgressReward => _goalDistanceProgressReward;
    public float DeadPenalty => _deadPenalty;
    public float HitPenalty => _hitPenalty;
    public float SurvivalRewardPerDecision => _survivalRewardPerDecision;
    public float PlatformStallPenaltyPerDecision => _platformStallPenaltyPerDecision;
    public float PlatformStallTimeSeconds => _platformStallTimeSeconds;
    public float MinLandingHeightDelta => _minLandingHeightDelta;
}
