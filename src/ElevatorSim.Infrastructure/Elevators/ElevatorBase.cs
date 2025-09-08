using ElevatorSim.Domain;

namespace ElevatorSim.Infrastructure.Elevators;

public abstract class ElevatorBase : IElevator
{
    protected readonly SortedSet<int> _upTargets = new SortedSet<int>();
    protected readonly SortedSet<int> _downTargets = new SortedSet<int>(Comparer<int>.Create((a, b) => b.CompareTo(a)));
    private readonly int _speedTicksPerFloor;
    private readonly int _doorOpenTicks;
    private readonly int _doorCloseTicks;
    protected readonly List<Passenger> _passengers = new();
    public IReadOnlyList<Passenger> Passengers => _passengers.AsReadOnly();
    public int AvailableCapacity => Math.Max(0, Capacity - _passengers.Count);
    public IElevatorObserver? Observer { get; set; }

    private int _moveTickBudget;
    private int _doorTickBudget;
    protected int TopFloorExclusive { get; private set; } = 12;
    public int SpeedTicksPerFloor { get; protected set; } = 5;

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
    public ElevatorType Type => ElevatorType.Passenger;
    public int CurrentFloor { get; protected set; }
    public Direction Direction { get; protected set; }
    public bool IsMoving => State == ElevatorState.DoorsClosed && Direction != Direction.Idle && HasTargets;
    public int Capacity { get; }
    public int PassengerCount => _passengers.Count;
    public ElevatorState State { get; private set; }
    public IReadOnlyList<int> Targets => GetTargetsSnapshot();

    protected bool HasTargets => _upTargets.Count > 0 || _downTargets.Count > 0;

    public virtual bool CanAccept(Request req)
    {
        return true;
    }

    public virtual void Assign(Request req)
    {
        if (req.Floor == CurrentFloor)
        {
            EnsureStopToServeCurrentFloor();
            return;
        }

        if (!AddTarget(req.Floor))
            return;

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
                    _doorTickBudget = Math.Max(1, _doorOpenTicks);
                    OnDoorsOpen();
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

                    OnDoorsClosed();

                    Direction = ChooseNextDirection();
                }
                break;
        }
    }


    public int BoardPassengers(IEnumerable<Passenger> boarding)
    {
        int boarded = 0;
        foreach (var p in boarding)
        {
            if (AvailableCapacity <= 0) break;
            _passengers.Add(p);
            AddTarget(p.DestinationFloor);
            boarded++;
        }
        if (Direction == Direction.Idle && HasTargets) ChooseInitialDirection();
        return boarded;
    }

    public int UnloadAtCurrentFloor()
    {
        var toUnload = _passengers.Where(p => p.DestinationFloor == CurrentFloor).ToList();

        foreach (var passenger in toUnload)
        {
            _passengers.Remove(passenger);
        }

        return toUnload.Count;
    }

    private void TickWhenDoorsClosed()
    {
        if (Direction == Direction.Idle)
        {
            Direction = ChooseInitialDirection();
            if (Direction == Direction.Idle) return;

            if (_moveTickBudget <= 0) _moveTickBudget = _speedTicksPerFloor;
        }

        if (--_moveTickBudget <= 0)
        {
            _moveTickBudget = _speedTicksPerFloor;

            if (Direction == Direction.Up)
            {
                if (CurrentFloor >= TopFloorExclusive - 1)
                {
                    _upTargets.Clear();
                    Direction = ChooseNextDirection();
                    return;
                }

                CurrentFloor++;

                if (_upTargets.Count > 0 && CurrentFloor == _upTargets.Min)
                {
                    _upTargets.Remove(_upTargets.Min);
                    BeginDoorOpen();
                    return;
                }
            }
            else if (Direction == Direction.Down)
            {
                if (CurrentFloor <= 0)
                {
                    _downTargets.Clear();
                    Direction = ChooseNextDirection();
                    return;
                }

                CurrentFloor--;

                if (_downTargets.Count > 0 && CurrentFloor == _downTargets.Max)
                {
                    _downTargets.Remove(_downTargets.Max);
                    BeginDoorOpen();
                    return;
                }
            }
        }
    }

    protected void BeginDoorOpen()
    {
        if (State == ElevatorState.DoorsOpening || State == ElevatorState.DoorsOpen)
            return;

        State = ElevatorState.DoorsOpening;
        _doorTickBudget = Math.Max(1, _doorOpenTicks);
        Observer?.OnArrivedAtFloor(this, CurrentFloor);
    }

    private void OnDoorsOpen()
    {
        Observer?.OnDoorsOpened(this, CurrentFloor);
    }


    private void OnDoorsClosed()
    {
        Observer?.OnDoorsClosed(this, CurrentFloor);
    }

    private void EnsureStopToServeCurrentFloor()
    {
        BeginDoorOpen();
    }

    private Direction ChooseInitialDirection()
    {
        if (_upTargets.Count > 0) return Direction.Up;
        if (_downTargets.Count > 0) return Direction.Down;
        return Direction.Idle;
    }

    private Direction ChooseNextDirection()
    {
        if (Direction == Direction.Up && _upTargets.Count > 0) return Direction.Up;
        if (Direction == Direction.Down && _downTargets.Count > 0) return Direction.Down;

        if (_upTargets.Count > 0) return Direction.Up;
        if (_downTargets.Count > 0) return Direction.Down;

        return Direction.Idle;
    }

    private IReadOnlyList<int> GetTargetsSnapshot()
    {
        var list = new List<int>(_upTargets.Count + _downTargets.Count);
        list.AddRange(_upTargets);
        list.AddRange(_downTargets);
        return list;
    }

    public bool AddTarget(int floor)
    {
        if (floor < 0 || floor >= TopFloorExclusive) return false;

        if (floor == CurrentFloor)
        {
            if (State == ElevatorState.DoorsClosed)
            {
                BeginDoorOpen();
                return true;
            }
            return false;
        }

        if (_upTargets.Contains(floor) || _downTargets.Contains(floor))
            return false;

        bool added = (floor > CurrentFloor) ? _upTargets.Add(floor) : _downTargets.Add(floor);
        if (!added) return false;

        if (Direction == Direction.Idle && State == ElevatorState.DoorsClosed)
        {
            Direction = ChooseInitialDirection();
            if (_moveTickBudget <= 0) _moveTickBudget = _speedTicksPerFloor;
        }

        return true;
    }

    public void PressButton(int floor)
    {
        if (floor < 0 || floor >= TopFloorExclusive) return;

        if (floor == CurrentFloor &&
            (State == ElevatorState.DoorsOpening || State == ElevatorState.DoorsOpen))
            return;

        AddTarget(floor);
    }

    protected void SetTopFloorExclusive(int floors)
    {
        TopFloorExclusive = Math.Max(1, floors);
    }
}