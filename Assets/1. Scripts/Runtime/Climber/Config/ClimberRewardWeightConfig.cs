using UnityEngine;

[CreateAssetMenu(fileName = "ClimberRewardWeightConfig", menuName = "GPE/Climber/Reward Weight Config")]
public sealed class ClimberRewardWeightConfig : ScriptableObject
{
    [SerializeField] private float _platformLandingReward = 1f;
    [SerializeField] private float _platformLandingDownPenalty = -0.5f;
    [SerializeField] private float _goalReachedReward = 10f;
    [SerializeField] private float _lavaContactPenalty = -10f;
    [SerializeField] private float _goalApproachRewardPerDecision = 0.02f;
    [SerializeField] private float _goalRecedePenaltyPerDecision = -0.005f;
    [SerializeField] private float _platformIdlePenaltyPerDecision = -0.001f;
    [SerializeField] private float _minLandingHeightDelta = 0.05f;
    [SerializeField] private float _goalDistanceYThreshold = 0.001f;

    public float PlatformLandingReward => _platformLandingReward;
    public float PlatformLandingDownPenalty => _platformLandingDownPenalty;
    public float GoalReachedReward => _goalReachedReward;
    public float LavaContactPenalty => _lavaContactPenalty;
    public float GoalApproachRewardPerDecision => _goalApproachRewardPerDecision;
    public float GoalRecedePenaltyPerDecision => _goalRecedePenaltyPerDecision;
    public float PlatformIdlePenaltyPerDecision => _platformIdlePenaltyPerDecision;
    public float MinLandingHeightDelta => _minLandingHeightDelta;
    public float GoalDistanceYThreshold => _goalDistanceYThreshold;
}
