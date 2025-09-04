using ElevatorSim.Domain;

namespace ElevatorSim.Infrastructure.Elevators;

public sealed class PassengerElevator : ElevatorBase
{
    public PassengerElevator(string id, int startFloor = 0, int capacity = 10, int speedTicksPerFloor = 2, int doorOpenTicks = 2, int doorCloseTicks = 2)
        : base(id, startFloor, capacity, speedTicksPerFloor, doorOpenTicks, doorCloseTicks)
    {
    }
}