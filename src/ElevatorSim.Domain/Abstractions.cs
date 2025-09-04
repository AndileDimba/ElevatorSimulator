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

    bool CanAccept(Request req);
    void Assign(Request req);
    void Tick();
}

public interface IDispatchStrategy
{
    IElevator? ChooseElevator(Building building, Request request);
}