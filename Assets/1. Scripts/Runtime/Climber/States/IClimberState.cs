public interface IClimberState
{
    ClimberStateId Id { get; }

    void Enter(ClimberStateContext context);

    void Exit(ClimberStateContext context);

    void Tick(ClimberStateContext context, ClimberMoveInput input);

    void FixedTick(ClimberStateContext context, ClimberMoveInput input);
}
