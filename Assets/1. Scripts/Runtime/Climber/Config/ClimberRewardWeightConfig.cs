using UnityEngine;

[CreateAssetMenu(fileName = "ClimberRewardWeightConfig", menuName = "GPE/Climber/Reward Weight Config")]
public sealed class ClimberRewardWeightConfig : ScriptableObject
{
    [SerializeField] private float _upwardProgressPerMeter = 1f;
    [SerializeField] private float _newBestHeightBonus = 0.5f;
    [SerializeField] private float _goalReachedReward = 10f;
    [SerializeField] private float _lavaContactPenalty = -5f;
    [SerializeField] private float _timeoutPenalty = -1f;
    [SerializeField] private float _stallPenaltyPerSecond = -0.1f;
    [SerializeField] private float _stallHeightEpsilon = 0.05f;
    [SerializeField] private float _stallTimeSeconds = 3f;
    [SerializeField] private float _smallTimePenaltyPerDecision = -0.0005f;
    [SerializeField] private float _stageHeightReference = 50f;
    [SerializeField] private float _stageWidthReference = 20f;

    public float UpwardProgressPerMeter => _upwardProgressPerMeter;
    public float NewBestHeightBonus => _newBestHeightBonus;
    public float GoalReachedReward => _goalReachedReward;
    public float LavaContactPenalty => _lavaContactPenalty;
    public float TimeoutPenalty => _timeoutPenalty;
    public float StallPenaltyPerSecond => _stallPenaltyPerSecond;
    public float StallHeightEpsilon => _stallHeightEpsilon;
    public float StallTimeSeconds => _stallTimeSeconds;
    public float SmallTimePenaltyPerDecision => _smallTimePenaltyPerDecision;
    public float StageHeightReference => _stageHeightReference;
    public float StageWidthReference => _stageWidthReference;
}
