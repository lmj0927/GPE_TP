public sealed class AirborneClimberState : IClimberState
{
    public ClimberStateId Id => ClimberStateId.Airborne;

    public void Enter(ClimberStateContext context)
    {
    }

    public void Exit(ClimberStateContext context)
    {
    }

    public void Tick(ClimberStateContext context, ClimberMoveInput input)
    {
        context.GroundChecker.Refresh();

        if (context.GroundChecker.IsGrounded)
            context.Agent.ChangeState(ClimberStateId.Grounded);
    }

    public void FixedTick(ClimberStateContext context, ClimberMoveInput input)
    {
        context.Motor.ApplyHorizontal(input.Horizontal, grounded: false, deltaTime: UnityEngine.Time.fixedDeltaTime);
    }
}
