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
    ElevatorState State { get; }
    IReadOnlyList<Passenger> Passengers { get; }
    int AvailableCapacity { get; }
    bool AddTarget(int floor);
    void PressButton(int floor);

    bool CanAccept(Request req);
    void Assign(Request req);
    void Tick();

    int UnloadAtCurrentFloor();
    int BoardPassengers(IEnumerable<Passenger> boarding);

    IElevatorObserver? Observer { get; set; }
}

public interface IDispatchStrategy
{
    IElevator? ChooseElevator(Building building, Request request);
}