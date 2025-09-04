using ElevatorSim.Domain;

namespace ElevatorSim.Infrastructure.Elevators;

// Temporary stub implementation so that solution will build.
// Day 2–3 will replace with ElevatorBase + concrete types as per my plan.
public sealed class StubElevator : IElevator
{
    private readonly List<int> _targets = new();

    public string Id { get; }
    public ElevatorType Type { get; }
    public int CurrentFloor { get; private set; }
    public Direction Direction { get; private set; } = Direction.Idle;
    public bool IsMoving => Direction != Direction.Idle && _targets.Count > 0;
    public int Capacity { get; }
    public int PassengerCount { get; private set; }
    public IReadOnlyList<int> Targets => _targets.AsReadOnly();

    public StubElevator(string id, int startFloor = 0, ElevatorType type = ElevatorType.Passenger, int capacity = 8)
    {
        Id = id;
        Type = type;
        CurrentFloor = startFloor;
        Capacity = capacity;
    }

    public bool CanAccept(Request req) => true;

    public void Assign(Request req)
    {
        if (!_targets.Contains(req.Floor))
            _targets.Add(req.Floor);
        Direction = _targets.Count > 0
            ? (_targets[0] > CurrentFloor ? Direction.Up : _targets[0] < CurrentFloor ? Direction.Down : Direction.Idle)
            : Direction.Idle;
    }

    public void Tick()
    {
        // Day 1: no movement, just keep targets. Real logic comes next.
    }
}