using UnityEngine;

public sealed class HitStunClimberState : IClimberState
{
    private float _remaining;

    public ClimberStateId Id => ClimberStateId.HitStun;

    public void Enter(ClimberStateContext context)
    {
        _remaining = context.Config.HitStunDuration;
    }

    public void Exit(ClimberStateContext context)
    {
        context.Agent.EndInvincibility();
    }

    public void FixedTick(ClimberStateContext context, ClimberMoveInput input)
    {
        context.GroundChecker.Refresh();
        _remaining -= Time.fixedDeltaTime;

        if (_remaining > 0f)
            return;

        context.Agent.ChangeState(
            context.GroundChecker.IsGrounded
                ? ClimberStateId.Grounded
                : ClimberStateId.Airborne);
    }
}
