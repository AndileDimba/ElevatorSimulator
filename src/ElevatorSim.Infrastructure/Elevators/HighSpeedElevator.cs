namespace ElevatorSim.Infrastructure.Elevators;

public sealed class HighSpeedElevator : PassengerElevator
{
    public HighSpeedElevator(string id, int startFloor = 0)
        : base(id, startFloor)
    {
        SpeedTicksPerFloor = 3;
    }
}