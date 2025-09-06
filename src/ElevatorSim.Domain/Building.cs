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

    // Existing API: submit a floor call (direction + count)
    public void SubmitCall(Request req)
    {
        ValidateFloor(req.Floor);

        // Day 3: create passenger placeholders in per-floor queues
        var fq = GetPassengerQueue(req.Floor);
        for (int i = 0; i < req.Count; i++)
        {
            // Simple destination heuristic for Day 3:
            // Up calls go a few floors up (min +1), down calls go a few floors down (max -1)
            int dest = req.Direction == Direction.Up
                ? Math.Min(Floors - 1, Math.Max(req.Floor + 1, req.Floor + 3))
                : Math.Max(0, Math.Min(req.Floor - 1, req.Floor - 3));

            fq.Enqueue(req.Direction, new Passenger(req.Floor, dest));
        }

        // Try immediate dispatch via strategy
        var chosen = DispatchStrategy.ChooseElevator(this, req);
        if (chosen != null && chosen.CanAccept(req))
        {
            chosen.Assign(req);
        }
        else
        {
            // Preserve the legacy request queue for informational purposes
            _floorRequestQueues[req.Floor].Enqueue(req);
        }
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
        // 1) Unload passengers whose destination == floor
        int unloaded = elevator.UnloadAtCurrentFloor();

        // 2) Load waiting passengers up to available capacity
        var fq = GetPassengerQueue(floor);
        var toBoard = new List<Passenger>();

        // Decide which direction to load:
        // - If elevator is moving, load that direction (keeps service efficient)
        // - If idle, choose the heavier queue; if both empty, nothing to do
        Direction dirToLoad = elevator.Direction;
        if (dirToLoad == Direction.Idle)
        {
            if (fq.Up.Count == 0 && fq.Down.Count == 0)
            {
                _events.Add($"Stop F:{floor} | Unloaded:{unloaded} Boarded:0 RemainingUp:{fq.Up.Count} RemainingDown:{fq.Down.Count}");
                return;
            }
            dirToLoad = fq.Up.Count >= fq.Down.Count ? Direction.Up : Direction.Down;
        }

        while (elevator.AvailableCapacity > 0)
        {
            var p = fq.TryDequeue(dirToLoad);
            if (p == null)
            {
                // If idle at open time and chosen dir is empty, try the other direction once
                if (elevator.Direction == Direction.Idle)
                {
                    var altDir = dirToLoad == Direction.Up ? Direction.Down : Direction.Up;
                    p = fq.TryDequeue(altDir);
                }

                if (p == null) break; // nothing to board
            }
            toBoard.Add(p);
        }

        int boarded = elevator.BoardPassengers(toBoard);

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