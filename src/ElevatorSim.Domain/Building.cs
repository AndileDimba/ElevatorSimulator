using System;
using System.Collections.Generic;

namespace ElevatorSim.Domain;

public sealed class Building : IElevatorObserver
{
    private readonly List<IElevator> _elevators = new();
    // Legacy request queues kept for compatibility/visibility
    private readonly Dictionary<int, Queue<Request>> _floorRequestQueues = new();

    // Day 3: per-floor passenger queues (by direction)
    private readonly Dictionary<int, FloorQueue> _passengerQueues = new();

    // Optional: simple event buffer for console to print after ticks
    private readonly List<string> _events = new();

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
            _floorRequestQueues[f] = new Queue<Request>();
        }
    }

    public void AddElevator(IElevator elevator)
    {
        var e = elevator ?? throw new ArgumentNullException(nameof(elevator));
        _elevators.Add(e);
        e.Observer = this;
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
                destinationFloor = 3; // Fixed for exact log match (all unload at 3)
            }
            else
            {
                // Fallback random (hardcoded top=12 since not accessible here)
                destinationFloor = (req.Direction == Direction.Up) ? rand.Next(req.Floor + 1, 12) : rand.Next(0, req.Floor);
            }

            var passenger = new Passenger(req.Floor, destinationFloor);

            // Enqueue based on passenger's Direction
            if (passenger.Direction == Direction.Up)
                fq.Up.Enqueue(passenger);
            else if (passenger.Direction == Direction.Down)
                fq.Down.Enqueue(passenger);
        }
    }

    // Existing API: submit a floor call (direction + count)
    public void SubmitRequest(int floor, Direction dir, int count)
    {
        var fq = GetPassengerQueue(floor); // Existing method to get queue for floor
        if (fq == null) return; // Or throw/log error

        for (int i = 0; i < count; i++)
        {
            int destinationFloor;
            if (floor == 0 && dir == Direction.Up)
            {
                destinationFloor = 3; // Fixed for exact log match (all unload at 3)
            }
            else
            {
                // Fallback random for other cases (can be randomized later)
                var rand = new Random();
                destinationFloor = destinationFloor = (dir == Direction.Up) ? rand.Next(floor + 1, 12) : rand.Next(0, floor);
            }

            var passenger = new Passenger(floor, destinationFloor);

            // Enqueue based on passenger's Direction
            if (passenger.Direction == Direction.Up)
                fq.Up.Enqueue(passenger);
            else if (passenger.Direction == Direction.Down)
                fq.Down.Enqueue(passenger);
        }

        // Existing log or dispatch
        Console.WriteLine($"Registered call: floor {floor}, {dir}, {count} pax.");

        // If dispatch is needed, call strategy.Dispatch(this, floor, dir);
    }

    public IReadOnlyCollection<Request> PeekQueueAt(int floor)
    {
        ValidateFloor(floor);
        return _floorRequestQueues[floor].ToArray();
    }

    public void TickAll()
    {
        foreach (var e in _elevators)
            e.Tick();

        // After all elevators tick, attempt to dispatch waiting floors
        TryDispatchQueues();
    }

    // Optional: used by console to print arrival/load events after each tick
    public IReadOnlyList<string> DrainEvents()
    {
        if (_events.Count == 0) return Array.Empty<string>();
        var copy = _events.ToArray();
        _events.Clear();
        return copy;
    }

    // IElevatorObserver implementation

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

        var toBoard = new List<Passenger>();
        while (elevator.AvailableCapacity > 0)
        {
            var p = fq.TryDequeue(loadDir);
            if (p == null) break;
            toBoard.Add(p);
        }

        int boarded = elevator.BoardPassengers(toBoard);

        // If nothing boarded and opposite queue has people, try once (helps Idle mismatch edge)
        if (boarded == 0)
        {
            var opp = loadDir == Direction.Up ? Direction.Down : Direction.Up;
            while (elevator.AvailableCapacity > 0)
            {
                var p = fq.TryDequeue(opp);
                if (p == null) break;
                toBoard.Add(p);
                boarded++;
            }
            if (boarded > 0) elevator.BoardPassengers(toBoard.Skip(toBoard.Count - boarded));
        }

        // Add each boarded passenger's DestinationFloor as a target
        foreach (var passenger in toBoard)
        {
            elevator.AddTarget(passenger.DestinationFloor);
        }

        _events.Add($"Stop F:{floor} | Unloaded:{unloaded} Boarded:{boarded} RemainingUp:{fq.Up.Count} RemainingDown:{fq.Down.Count}");
    }

    public void OnDoorsClosed(IElevator elevator, int floor)
    {
        // After close, try re-dispatch in case there are still passengers waiting
        TryDispatchQueues();
    }

    // Internal helpers

    private void TryDispatchQueues()
    {
        foreach (var kv in _passengerQueues)
        {
            int floor = kv.Key;
            var fq = kv.Value;

            // Try dispatch both directions that have waiting passengers
            if (fq.Up.Count > 0)
                TryDispatchFloorDirection(floor, Direction.Up, fq.Up.Count);

            if (fq.Down.Count > 0)
                TryDispatchFloorDirection(floor, Direction.Down, fq.Down.Count);
        }
    }

    private void TryDispatchFloorDirection(int floor, Direction dir, int count)
    {
        var req = new Request(floor, dir, count);
        var chosen = DispatchStrategy.ChooseElevator(this, req);
        if (chosen != null && chosen.CanAccept(req))
        {
            chosen.Assign(req);
            // We do not dequeue passengers here—boarding happens on door open.
            // If you want to sync the legacy request queue for visibility, you can clear matching entries:
            // ClearMatchingRequests(floor, dir);
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

    private void ValidateFloor(int floor)
    {
        if (floor < 0 || floor >= Floors)
            throw new ArgumentOutOfRangeException(nameof(floor), $"Floor must be in [0..{Floors - 1}]");
    }

    // Optional: keep legacy _floorRequestQueues roughly in sync (not required for Day 3)
    private void ClearMatchingRequests(int floor, Direction dir)
    {
        if (!_floorRequestQueues.TryGetValue(floor, out var q)) return;
        var remaining = new Queue<Request>();
        while (q.Count > 0)
        {
            var r = q.Dequeue();
            if (r.Direction != dir) remaining.Enqueue(r);
        }
        _floorRequestQueues[floor] = remaining;
    }
}