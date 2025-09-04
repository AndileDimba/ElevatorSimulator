using ElevatorSim.Domain;

namespace ElevatorSim.Infrastructure.Elevators;

public abstract class ElevatorBase : IElevator
{
    private readonly SortedSet<int> _upTargets = new();      // ascending
    private readonly SortedSet<int> _downTargets = new DescendingIntSet(); // descending
    private readonly int _speedTicksPerFloor;
    private readonly int _doorOpenTicks;
    private readonly int _doorCloseTicks;

    private int _moveTickBudget;   // counts down; when zero, move one floor
    private int _doorTickBudget;   // counts down during opening/open/closing

    protected ElevatorBase(string id, int startFloor, int capacity, int speedTicksPerFloor, int doorOpenTicks, int doorCloseTicks)
    {
        Id = id;
        CurrentFloor = startFloor;
        Capacity = capacity;
        _speedTicksPerFloor = Math.Max(1, speedTicksPerFloor);
        _doorOpenTicks = Math.Max(1, doorOpenTicks);
        _doorCloseTicks = Math.Max(1, doorCloseTicks);
        Direction = Direction.Idle;
        State = ElevatorState.DoorsClosed;
        _moveTickBudget = _speedTicksPerFloor;
    }

    public string Id { get; }
    public ElevatorType Type => ElevatorType.Passenger; // overridden by subclasses if needed
    public int CurrentFloor { get; protected set; }
    public Direction Direction { get; protected set; }
    public bool IsMoving => State == ElevatorState.DoorsClosed && Direction != Direction.Idle && HasTargets;
    public int Capacity { get; }
    public int PassengerCount { get; protected set; }
    public ElevatorState State { get; private set; }
    public IReadOnlyList<int> Targets => GetTargetsSnapshot();

    protected bool HasTargets => _upTargets.Count > 0 || _downTargets.Count > 0;

    public virtual bool CanAccept(Request req)
    {
        // Accept everything on Day 2. Day 3 will refine based on direction/capacity.
        return true;
    }

    public virtual void Assign(Request req)
    {
        if (req.Floor == CurrentFloor)
        {
            // If already here, ensure we stop and open doors next tick
            EnsureStopToServeCurrentFloor();
            return;
        }

        if (req.Floor > CurrentFloor)
            _upTargets.Add(req.Floor);
        else
            _downTargets.Add(req.Floor);

        // If idle, choose direction toward nearest target
        if (Direction == Direction.Idle && State == ElevatorState.DoorsClosed)
        {
            Direction = ChooseInitialDirection();
        }
    }

    public void Tick()
    {
        switch (State)
        {
            case ElevatorState.DoorsClosed:
                TickWhenDoorsClosed();
                break;
            case ElevatorState.DoorsOpening:
                if (--_doorTickBudget <= 0)
                {
                    State = ElevatorState.DoorsOpen;
                    _doorTickBudget = Math.Max(1, _doorOpenTicks); // dwell time open
                    // Simulate unload/load at arrival floor (Day 3 will handle capacity)
                    OnArriveAndServeFloor();
                }
                break;
            case ElevatorState.DoorsOpen:
                if (--_doorTickBudget <= 0)
                {
                    State = ElevatorState.DoorsClosing;
                    _doorTickBudget = Math.Max(1, _doorCloseTicks);
                }
                break;
            case ElevatorState.DoorsClosing:
                if (--_doorTickBudget <= 0)
                {
                    State = ElevatorState.DoorsClosed;
                    // After closing, decide next direction
                    Direction = ChooseNextDirection();
                }
                break;
        }
    }

    protected virtual void OnArriveAndServeFloor()
    {
        // Remove current floor from targets if present
        _upTargets.Remove(CurrentFloor);
        _downTargets.Remove(CurrentFloor);
        // Passenger load/unload happens on Day 3
    }

    private void TickWhenDoorsClosed()
    {
        if (!HasTargets)
        {
            Direction = Direction.Idle;
            return;
        }

        // If at a target floor, open doors
        if (IsTarget(CurrentFloor))
        {
            BeginDoorOpen();
            return;
        }

        // Otherwise move toward current direction’s nearest target
        if (--_moveTickBudget <= 0)
        {
            _moveTickBudget = _speedTicksPerFloor;
            if (Direction == Direction.Up) CurrentFloor++;
            else if (Direction == Direction.Down) CurrentFloor--;
        }

        // If we just reached a target, open doors next opportunity
        if (IsTarget(CurrentFloor))
        {
            BeginDoorOpen();
        }
        else
        {
            // If moving in a direction with no remaining targets ahead, flip if other side has targets
            if (Direction == Direction.Up && !_upTargets.Any(t => t > CurrentFloor) && _downTargets.Count > 0)
                Direction = Direction.Down;
            else if (Direction == Direction.Down && !_downTargets.Any(t => t < CurrentFloor) && _upTargets.Count > 0)
                Direction = Direction.Up;
        }
    }

    private void BeginDoorOpen()
    {
        State = ElevatorState.DoorsOpening;
        _doorTickBudget = Math.Max(1, _doorOpenTicks);
        // While doors opening/open, elevator is considered not moving
    }

    private void EnsureStopToServeCurrentFloor()
    {
        // If we’re already at the requested floor, begin the door cycle so the floor is served.
        // Reset movement budget so we don’t accidentally move before serving.
        State = ElevatorState.DoorsOpening;
        _doorTickBudget = Math.Max(1, _doorOpenTicks);
        _moveTickBudget = _speedTicksPerFloor;

        // Remove current floor from targets in case it’s present.
        _upTargets.Remove(CurrentFloor);
        _downTargets.Remove(CurrentFloor);

        // Direction can remain as-is; once doors close, ChooseNextDirection() will decide.
    }

    private Direction ChooseInitialDirection()
    {
        if (_upTargets.Count == 0 && _downTargets.Count == 0) return Direction.Idle;
        if (_upTargets.Count == 0) return Direction.Down;
        if (_downTargets.Count == 0) return Direction.Up;

        // Choose nearest
        int nearestUp = _upTargets.Min;
        int nearestDown = _downTargets.Max; // due to descending set
        int dUp = Math.Abs(nearestUp - CurrentFloor);
        int dDown = Math.Abs(CurrentFloor - nearestDown);
        return dUp <= dDown ? Direction.Up : Direction.Down;
    }

    private Direction ChooseNextDirection()
    {
        // If still targets at current floor after serving, decide next direction by availability
        if (_upTargets.Count == 0 && _downTargets.Count == 0) return Direction.Idle;

        // Prefer continuing in same direction if targets remain that way
        if (Direction == Direction.Up && _upTargets.Any(t => t >= CurrentFloor)) return Direction.Up;
        if (Direction == Direction.Down && _downTargets.Any(t => t <= CurrentFloor)) return Direction.Down;

        // Otherwise choose side with nearest target
        return ChooseInitialDirection();
    }

    private bool IsTarget(int floor) => _upTargets.Contains(floor) || _downTargets.Contains(floor);

    private IReadOnlyList<int> GetTargetsSnapshot()
    {
        var list = new List<int>(_upTargets.Count + _downTargets.Count);
        list.AddRange(_upTargets);
        list.AddRange(_downTargets);
        return list;
    }

    // A descending sorted set for down targets
    private sealed class DescendingIntSet : SortedSet<int>
    {
        public DescendingIntSet() : base(Comparer<int>.Create((a, b) => b.CompareTo(a))) { }
    }
}