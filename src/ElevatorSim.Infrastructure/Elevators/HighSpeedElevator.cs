// HighSpeedElevator.cs
namespace ElevatorSim.Infrastructure.Elevators;

public sealed class HighSpeedElevator : PassengerElevator
{
    public HighSpeedElevator(string id, int startFloor = 0)
        : base(id, startFloor) // match your current PassengerElevator constructor
    {
        // relies on ElevatorBase exposing protected setter
        SpeedTicksPerFloor = 3;
    }
}