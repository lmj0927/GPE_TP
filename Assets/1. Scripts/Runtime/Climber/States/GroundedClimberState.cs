using UnityEngine;

public sealed class GroundedClimberState : IClimberState
{
    public ClimberStateId Id => ClimberStateId.Grounded;

    public void Enter(ClimberStateContext context)
    {
        context.Motor.ResetJumpOnLanding();
    }

    public void Exit(ClimberStateContext context)
    {
    }

    public void FixedTick(ClimberStateContext context, ClimberMoveInput input)
    {
        context.GroundChecker.Refresh();
        float deltaTime = Time.fixedDeltaTime;

        if (!context.GroundChecker.IsGrounded)
        {
            context.Agent.ChangeState(ClimberStateId.Airborne);
            context.Motor.ApplyHorizontal(input.Horizontal, grounded: false, deltaTime);
            return;
        }

        if (input.Jump && context.Motor.CanJump && context.Motor.TryJump())
        {
            context.Agent.ClearJumpBuffer();
            context.Agent.ChangeState(ClimberStateId.Airborne);
            context.Motor.ApplyHorizontal(input.Horizontal, grounded: false, deltaTime);
            return;
        }

        context.Motor.ApplyHorizontal(input.Horizontal, grounded: true, deltaTime);
    }
}
