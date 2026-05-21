public interface IClimberState
{
    ClimberStateId Id { get; }

    void Enter(ClimberStateContext context);

    void Exit(ClimberStateContext context);

    void FixedTick(ClimberStateContext context, ClimberMoveInput input);
}
