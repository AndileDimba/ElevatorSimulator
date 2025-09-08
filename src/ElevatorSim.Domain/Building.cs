using System;
using System.Collections.Generic;

namespace ElevatorSim.Domain;

public sealed class Building : IElevatorObserver
{
    private readonly List<IElevator> _elevators = new();
    private readonly Dictionary<int, Queue<Request>> _floorRequestQueues = new();
    private readonly Dictionary<int, FloorQueue> _passengerQueues = new();
    private readonly List<string> _events = new();
    private readonly HashSet<int> _floorsBoardedThisTick = new();
    private readonly HashSet<string> _outOfService = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(int Floor, Direction Dir), string> _reservations = new();
    private readonly Dictionary<(int Floor, Direction Dir), Queue<int>> _waitingCreatedTicks = new();

    public int Floors { get; }
    private int _tick;
    private long _sumWaitTicks;
    private int _servedPassengers;
    private int _maxWaitTicks;
    public int CurrentTick => _tick;

    public IReadOnlyList<IElevator> Elevators => _elevators.AsReadOnly();
    public IDispatchStrategy DispatchStrategy { get; }
    public bool IsOutOfService(string elevatorId) => _outOfService.Contains(elevatorId);
    public IReadOnlyCollection<string> OutOfServiceElevators => _outOfService.ToArray();

    public Building(int floors, IDispatchStrategy dispatchStrategy)
    {
        if (floors < 2) throw new ArgumentOutOfRangeException(nameof(floors), "Building must have at least 2 floors.");
        Floors = floors;
        DispatchStrategy = dispatchStrategy ?? throw new ArgumentNullException(nameof(dispatchStrategy));

        for (int f = 0; f < Floors; f++)
        {
            _floorRequestQueues[f] = new Queue<Request>();
        }
    }

    public bool AddElevator(IElevator elevator)
    {
        if (_elevators.Any(e => e.Id.Equals(elevator.Id, StringComparison.OrdinalIgnoreCase)))
            return false;
        _elevators.Add(elevator);
        return true;
    }

    public void SubmitCall(Request req)
    {
        var fq = GetPassengerQueue(req.Floor);
        if (fq == null) throw new InvalidOperationException($"No queue for floor {req.Floor}");

        var rand = new Random();
        for (int i = 0; i < req.Count; i++)
        {
            int destinationFloor;
            if (req.Floor == 0 && req.Direction == Direction.Up)
            {
                destinationFloor = 3;
            }
            else
            {
                destinationFloor = (req.Direction == Direction.Up) ? rand.Next(req.Floor + 1, 12) : rand.Next(0, req.Floor);
            }

            var passenger = new Passenger(req.Floor, destinationFloor);

            if (passenger.Direction == Direction.Up)
            {
                fq.Up.Enqueue(passenger);
                StampWaitCreated(req.Floor, Direction.Up);
            }
            else if (passenger.Direction == Direction.Down)
            {
                fq.Down.Enqueue(passenger);
                StampWaitCreated(req.Floor, Direction.Down);
            }
        }
    }

    public void SubmitRequest(int floor, Direction dir, int count)
    {
        SubmitCall(new Request(floor, dir, count));
    }

    public void TickAll()
    {
        _floorsBoardedThisTick.Clear();

        foreach (var e in _elevators)
        {
            var prevState = e.State;
            e.Tick();

            if (prevState != ElevatorState.DoorsOpen && e.State == ElevatorState.DoorsOpen)
            {
                if (_floorsBoardedThisTick.Add(e.CurrentFloor))
                {
                    OnDoorsOpened(e, e.CurrentFloor);
                }
            }
        }

        TryDispatchQueues();
        _tick++;
    }

    public IReadOnlyList<string> DrainEvents()
    {
        if (_events.Count == 0) return Array.Empty<string>();
        var copy = _events.ToArray();
        _events.Clear();
        return copy;
    }

    public int GetWaitingCount(int floor, Direction direction)
    {
        var fq = GetPassengerQueue(floor);
        if (fq == null) return 0;
        return direction == Direction.Up ? fq.Up.Count : fq.Down.Count;
    }


    public void OnArrivedAtFloor(IElevator elevator, int floor)
    {
        _events.Add($"Arrived: {elevator.Id} at F:{floor}");
    }

    public void OnDoorsOpened(IElevator elevator, int floor)
    {
        int unloaded = elevator.UnloadAtCurrentFloor();

        var fq = GetPassengerQueue(floor);
        if (fq == null)
        {
            _events.Add($"Stop F:{floor} | Unloaded:{unloaded} Boarded:0 RemainingUp:0 RemainingDown:0");
            return;
        }

        Direction loadDir = elevator.Direction;
        if (loadDir == Direction.Idle)
            loadDir = fq.Up.Count >= fq.Down.Count ? Direction.Up : Direction.Down;

        if ((loadDir == Direction.Up && fq.Up.Count == 0) ||
            (loadDir == Direction.Down && fq.Down.Count == 0))
        {
            loadDir = (loadDir == Direction.Up) ? Direction.Down : Direction.Up;
        }

        var primary = new List<Passenger>();
        while (primary.Count < elevator.AvailableCapacity)
        {
            var p = fq.TryDequeue(loadDir);
            if (p == null) break;
            primary.Add(p);
        }

        int boardedPrimary = 0;
        if (primary.Count > 0)
        {
            boardedPrimary = elevator.BoardPassengers(primary);
            foreach (var passenger in primary.Take(boardedPrimary))
                elevator.AddTarget(passenger.DestinationFloor);

            AccumulateWaits(floor, loadDir, boardedPrimary);
        }

        int boardedOpp = 0;
        if (boardedPrimary == 0)
        {
            var opp = (loadDir == Direction.Up) ? Direction.Down : Direction.Up;
            var opposite = new List<Passenger>();
            while (opposite.Count < elevator.AvailableCapacity)
            {
                var p = fq.TryDequeue(opp);
                if (p == null) break;
                opposite.Add(p);
            }

            if (opposite.Count > 0)
            {
                boardedOpp = elevator.BoardPassengers(opposite);
                foreach (var passenger in opposite.Take(boardedOpp))
                    elevator.AddTarget(passenger.DestinationFloor);

                AccumulateWaits(floor, opp, boardedOpp);
            }
        }

        int boarded = boardedPrimary + boardedOpp;

        _events.Add($"Stop F:{floor} | Unloaded:{unloaded} Boarded:{boarded} RemainingUp:{fq.Up.Count} RemainingDown:{fq.Down.Count}");
    }

    private void AccumulateWaits(int floor, Direction dir, int boardedCount)
    {
        if (boardedCount <= 0) return;

        var key = (floor, dir);
        if (!_waitingCreatedTicks.TryGetValue(key, out var createdQ)) return;

        for (int i = 0; i < boardedCount && createdQ.Count > 0; i++)
        {
            int created = createdQ.Dequeue();
            int wait = _tick - created;
            _servedPassengers++;
            _sumWaitTicks += wait;
            if (wait > _maxWaitTicks) _maxWaitTicks = wait;
        }

        if (createdQ.Count == 0) _waitingCreatedTicks.Remove(key);
    }

    private void StampWaitCreated(int floor, Direction dir)
    {
        var key = (floor, dir);
        if (!_waitingCreatedTicks.TryGetValue(key, out var q))
        {
            q = new Queue<int>();
            _waitingCreatedTicks[key] = q;
        }
        q.Enqueue(_tick);
    }

    public void OnDoorsClosed(IElevator elevator, int floor)
    {
        TryDispatchQueues();
    }

    private void TryDispatchQueues()
    {
        foreach (var kv in _passengerQueues)
        {
            int floor = kv.Key;
            var fq = kv.Value;

            if (fq.Up.Count > 0)
                TryDispatchFloorDirection(floor, Direction.Up, fq.Up.Count);

            if (fq.Down.Count > 0)
                TryDispatchFloorDirection(floor, Direction.Down, fq.Down.Count);
        }
    }

    private void TryDispatchFloorDirection(int floor, Direction dir, int count)
    {
        if (_reservations.TryGetValue((floor, dir), out var reservedFor))
        {
            if (_elevators.Any(e => e.Id.Equals(reservedFor, StringComparison.OrdinalIgnoreCase) && !IsOutOfService(e.Id)))
                return;

            _reservations.Remove((floor, dir));
        }

        var req = new Request(floor, dir, count);
        var chosen = DispatchStrategy.ChooseElevator(this, req);
        if (chosen == null) return;

        var capacity = chosen.AvailableCapacity;
        if (capacity <= 0 && chosen.CurrentFloor != floor) return;

        var assignCount = capacity > 0 ? Math.Min(count, capacity) : 1;
        var reqToAssign = (assignCount == count) ? req : new Request(floor, dir, assignCount);

        if (chosen.CanAccept(reqToAssign))
        {
            chosen.Assign(reqToAssign);
            _reservations[(floor, dir)] = chosen.Id;
        }
    }

    private FloorQueue GetPassengerQueue(int floor)
    {
        if (!_passengerQueues.TryGetValue(floor, out var fq))
        {
            fq = new FloorQueue(floor);
            _passengerQueues[floor] = fq;
        }
        return fq;
    }

    public bool SetOutOfService(string elevatorId, bool value)
    {
        var exists = _elevators.Any(e => e.Id.Equals(elevatorId, StringComparison.OrdinalIgnoreCase));
        if (!exists) return false;
        if (value) _outOfService.Add(elevatorId);
        else _outOfService.Remove(elevatorId);

        _events.Add($"OOS {(value ? "ON" : "OFF")}: {elevatorId}");
        return true;
    }

    public IEnumerable<string> GetRecentEvents(int take = 20)
    {
        if (take <= 0) yield break;
        int start = Math.Max(0, _events.Count - take);
        for (int i = start; i < _events.Count; i++)
            yield return _events[i];
    }

    public (int served, double avgWait, int maxWait) GetWaitMetrics()
    {
        var avg = _servedPassengers > 0 ? (double)_sumWaitTicks / _servedPassengers : 0.0;
        return (_servedPassengers, avg, _maxWaitTicks);
    }

    public void ResetWaitMetrics()
    {
        _sumWaitTicks = 0;
        _servedPassengers = 0;
        _maxWaitTicks = 0;
        _waitingCreatedTicks.Clear();
        _reservations.Clear();
    }
}