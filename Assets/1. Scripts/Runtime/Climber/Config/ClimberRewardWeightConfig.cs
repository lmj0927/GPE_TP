using UnityEngine;

[CreateAssetMenu(fileName = "ClimberRewardWeightConfig", menuName = "GPE/Climber/Reward Weight Config")]
public sealed class ClimberRewardWeightConfig : ScriptableObject
{
    [SerializeField] private float _platformLandingReward = 1f;
    [SerializeField] private float _platformLandingRecoveryReward = 0.3f;
    [SerializeField] private float _platformLandingDownPenalty = -0.5f;
    [SerializeField] private float _goalReachedReward = 10f;
    [SerializeField] private float _goalDistanceProgressReward = 0.2f;
    [SerializeField] private float _lavaContactPenalty = -10f;
    [SerializeField] private float _platformStallPenaltyPerDecision = -0.001f;
    [SerializeField] private float _platformStallTimeSeconds = 3f;
    [SerializeField] private float _minLandingHeightDelta = 0.05f;

    public float PlatformLandingReward => _platformLandingReward;
    public float PlatformLandingRecoveryReward => _platformLandingRecoveryReward;
    public float PlatformLandingDownPenalty => _platformLandingDownPenalty;
    public float GoalReachedReward => _goalReachedReward;
    public float GoalDistanceProgressReward => _goalDistanceProgressReward;
    public float LavaContactPenalty => _lavaContactPenalty;
    public float PlatformStallPenaltyPerDecision => _platformStallPenaltyPerDecision;
    public float PlatformStallTimeSeconds => _platformStallTimeSeconds;
    public float MinLandingHeightDelta => _minLandingHeightDelta;
}
