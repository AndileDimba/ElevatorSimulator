namespace ElevatorSim.Infrastructure.Elevators;

public sealed class FreightElevator : PassengerElevator
{
    public FreightElevator(string id, int startFloor = 0)
        : base(id, startFloor)
    {
    }
}