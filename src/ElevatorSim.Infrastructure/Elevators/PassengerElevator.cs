using ElevatorSim.Domain;

namespace ElevatorSim.Infrastructure.Elevators;

public sealed class PassengerElevator : ElevatorBase
{
    // 1-arg overload for tests: (id) - defaults startFloor to 0
    public PassengerElevator(string id)
        : this(id, startFloor: 0)
    { }

    // 2-arg overload for tests/Program.cs: (id, startFloor)
    public PassengerElevator(string id, int startFloor)
        : this(id, startFloor, capacity: 10, speedTicksPerFloor: 5, doorOpenTicks: 2, doorCloseTicks: 2, floors: 12)
    { }

    // 3-arg overload for tests: (id, startFloor, speedTicksPerFloor)
    public PassengerElevator(string id, int startFloor, int speedTicksPerFloor)
        : this(id, startFloor, capacity: 10, speedTicksPerFloor: speedTicksPerFloor, doorOpenTicks: 2, doorCloseTicks: 2, floors: 12)
    { }

    // Full-parameter constructor (used by all overloads, with defaults for door timings)
    public PassengerElevator(
        string id,
        int startFloor,
        int capacity,
        int speedTicksPerFloor,
        int doorOpenTicks = 2,  // Default value
        int doorCloseTicks = 2, // Default value
        int floors = 12
    )
        : base(id, startFloor, capacity, speedTicksPerFloor, doorOpenTicks, doorCloseTicks)
    {
        SetTopFloorExclusive(floors);
    }
}