namespace ElevatorSim.Domain;

public interface IElevator
{
    string Id { get; }
    ElevatorType Type { get; }
    int CurrentFloor { get; }
    Direction Direction { get; }
    bool IsMoving { get; }
    int Capacity { get; }
    int PassengerCount { get; }
    IReadOnlyList<int> Targets { get; }
    ElevatorState State { get; } // To expose State from ElevatorBase via IElevator
    IReadOnlyList<Passenger> Passengers { get; }
    int AvailableCapacity { get; }

    bool CanAccept(Request req);
    void Assign(Request req);
    void Tick();

    // Day 3: operations needed by Building without referencing Infrastructure
    int UnloadAtCurrentFloor();
    int BoardPassengers(IEnumerable<Passenger> boarding);

    // Day 3: observer hook
    IElevatorObserver? Observer { get; set; }
}

public interface IDispatchStrategy
{
    IElevator? ChooseElevator(Building building, Request request);
}