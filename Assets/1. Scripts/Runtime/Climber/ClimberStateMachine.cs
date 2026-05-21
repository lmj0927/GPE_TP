using System.Collections.Generic;

public sealed class ClimberStateMachine
{
    private readonly Dictionary<ClimberStateId, IClimberState> _states = new();
    private readonly ClimberStateContext _context;

    public ClimberStateId CurrentId { get; private set; }

    private IClimberState _current;

    public ClimberStateMachine(ClimberStateContext context)
    {
        _context = context;
        _states[ClimberStateId.Grounded] = new GroundedClimberState();
        _states[ClimberStateId.Airborne] = new AirborneClimberState();
        _states[ClimberStateId.HitStun] = new HitStunClimberState();
    }

    public void Initialize(ClimberStateId startId)
    {
        CurrentId = startId;
        _current = _states[startId];
        _current.Enter(_context);
    }

    public void ChangeState(ClimberStateId nextId)
    {
        if (CurrentId == nextId)
            return;

        _current.Exit(_context);
        CurrentId = nextId;
        _current = _states[nextId];
        _current.Enter(_context);
    }

    public void FixedTick(ClimberMoveInput input) => _current.FixedTick(_context, input);
}
