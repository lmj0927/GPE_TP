using UnityEngine;

public sealed class AirborneClimberState : IClimberState
{
    public ClimberStateId Id => ClimberStateId.Airborne;

    public void Enter(ClimberStateContext context)
    {
    }

    public void Exit(ClimberStateContext context)
    {
    }

    public void FixedTick(ClimberStateContext context, ClimberMoveInput input)
    {
        context.GroundChecker.Refresh();

        if (context.GroundChecker.IsGrounded)
            context.Agent.ChangeState(ClimberStateId.Grounded);

        context.Motor.ApplyHorizontal(input.Horizontal, grounded: false, Time.fixedDeltaTime);
    }
}
