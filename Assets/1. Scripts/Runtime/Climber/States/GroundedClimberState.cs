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

    public void Tick(ClimberStateContext context, ClimberMoveInput input)
    {
        context.GroundChecker.Refresh();

        if (!context.GroundChecker.IsGrounded)
        {
            context.Agent.ChangeState(ClimberStateId.Airborne);
            return;
        }

        if (input.Jump && context.Motor.CanJump && context.Motor.TryJump())
        {
            context.Agent.ClearJumpBuffer();
            context.Agent.ChangeState(ClimberStateId.Airborne);
        }
    }

    public void FixedTick(ClimberStateContext context, ClimberMoveInput input)
    {
        context.Motor.ApplyHorizontal(input.Horizontal, grounded: true, deltaTime: UnityEngine.Time.fixedDeltaTime);
    }
}
