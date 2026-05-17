public sealed class ClimberStateContext
{
    public EnemyAgent Agent { get; }
    public ClimberMotor Motor { get; }
    public GroundChecker GroundChecker { get; }
    public ClimberMovementConfig Config { get; }

    public ClimberStateContext(
        EnemyAgent agent,
        ClimberMotor motor,
        GroundChecker groundChecker,
        ClimberMovementConfig config)
    {
        Agent = agent;
        Motor = motor;
        GroundChecker = groundChecker;
        Config = config;
    }
}
