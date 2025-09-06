namespace ElevatorSim.Infrastructure.Elevators;

public sealed class FreightElevator : PassengerElevator
{
    public FreightElevator(string id, int startFloor = 0)
        : base(id, startFloor)
    {
        // For now, just a distinct type tag. If you later want to customize capacity/speed/door timings,
        // we’ll add a protected constructor or protected setters in ElevatorBase/PassengerElevator.
    }
}