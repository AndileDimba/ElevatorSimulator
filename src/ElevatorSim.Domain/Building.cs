namespace ElevatorSim.Domain;

public sealed class Building
{
    private readonly List<IElevator> _elevators = new();
    private readonly Dictionary<int, Queue<Request>> _floorQueues = new();

    public int Floors { get; }
    public IReadOnlyList<IElevator> Elevators => _elevators.AsReadOnly();

    public IDispatchStrategy DispatchStrategy { get; }

    public Building(int floors, IDispatchStrategy dispatchStrategy)
    {
        if (floors < 2) throw new ArgumentOutOfRangeException(nameof(floors), "Building must have at least 2 floors.");
        Floors = floors;
        DispatchStrategy = dispatchStrategy ?? throw new ArgumentNullException(nameof(dispatchStrategy));

        for (int f = 0; f < Floors; f++)
        {
            _floorQueues[f] = new Queue<Request>();
        }
    }

    public void AddElevator(IElevator elevator)
    {
        _elevators.Add(elevator ?? throw new ArgumentNullException(nameof(elevator)));
    }

    public void SubmitCall(Request req)
    {
        ValidateFloor(req.Floor);
        var chosen = DispatchStrategy.ChooseElevator(this, req);

        if (chosen != null && chosen.CanAccept(req))
        {
            chosen.Assign(req);
        }
        else
        {
            _floorQueues[req.Floor].Enqueue(req);
        }
    }

    public IReadOnlyCollection<Request> PeekQueueAt(int floor)
    {
        ValidateFloor(floor);
        return _floorQueues[floor].ToArray();
    }

    public void TickAll()
    {
        foreach (var e in _elevators)
            e.Tick();

        // Future: attempt to dispatch queued requests again after ticks
    }

    private void ValidateFloor(int floor)
    {
        if (floor < 0 || floor >= Floors)
            throw new ArgumentOutOfRangeException(nameof(floor), $"Floor must be in [0..{Floors - 1}]");
    }
}