using UnityEngine;

/// <summary>
/// Shared climber surface for FSM states, obstacles, and stage triggers.
/// </summary>
public interface IClimberAgent
{
    ClimberStateId CurrentState { get; }
    Vector2 WorldPosition { get; }
    Vector2 WorldVelocity { get; }

    void ChangeState(ClimberStateId stateId);
    void ClearJumpBuffer();
    void EndInvincibility();
    void ApplyHit();
    void NotifyGoalReached();
    void NotifyLavaContact();
}
