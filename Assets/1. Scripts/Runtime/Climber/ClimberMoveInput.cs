public readonly struct ClimberMoveInput
{
    public readonly float Horizontal;
    public readonly bool Jump;

    public ClimberMoveInput(float horizontal, bool jump)
    {
        Horizontal = horizontal;
        Jump = jump;
    }

    public static ClimberMoveInput Zero => new(0f, false);
}
