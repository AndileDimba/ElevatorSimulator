using ElevatorSim.Domain;

namespace ElevatorSim.Infrastructure.Elevators;

public class PassengerElevator : ElevatorBase
{
    public PassengerElevator(string id)
        : this(id, startFloor: 0)
    { }

    public PassengerElevator(string id, int startFloor)
        : this(id, startFloor, capacity: 10, speedTicksPerFloor: 5, doorOpenTicks: 2, doorCloseTicks: 2, floors: 12)
    { }

    public PassengerElevator(string id, int startFloor, int speedTicksPerFloor)
        : this(id, startFloor, capacity: 10, speedTicksPerFloor: speedTicksPerFloor, doorOpenTicks: 2, doorCloseTicks: 2, floors: 12)
    { }

    public PassengerElevator(
        string id,
        int startFloor,
        int capacity,
        int speedTicksPerFloor,
        int doorOpenTicks = 2,
        int doorCloseTicks = 2,
        int floors = 12
    )
        : base(id, startFloor, capacity, speedTicksPerFloor, doorOpenTicks, doorCloseTicks)
    {
        SetTopFloorExclusive(floors);
    }
}