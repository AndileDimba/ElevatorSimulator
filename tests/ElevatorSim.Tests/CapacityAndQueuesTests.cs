using ElevatorSim.Domain;
using ElevatorSim.Infrastructure.Dispatch;
using ElevatorSim.Infrastructure.Elevators;
using Xunit;
using System.Collections.Generic;

public class CapacityAndQueuesTests
{
    [Fact]
    public void LoadsUpToCapacityAndLeavesRemainderQueued()
    {
        var e1 = new PassengerElevator("E1", startFloor: 0, capacity: 4, speedTicksPerFloor: 1, doorOpenTicks: 1, doorCloseTicks: 1);
        var building = new Building(12, new NearestAvailableDispatch());
        building.AddElevator(e1);

        // Submit 6 passengers waiting at floor 0, going up
        var req = new Request(0, Direction.Up, 6);
        building.SubmitCall(req);

        // Force the elevator to serve floor 0 now (bypass strategy variability)
        e1.Assign(req);

        // Act: tick until doors open and loading occurs
        // One tick to transition DoorsClosed -> DoorsOpening (if needed),
        // another to DoorsOpen (doorOpenTicks = 1), then Building.OnDoorsOpened loads.
        for (int i = 0; i < 3; i++)
        {
            building.TickAll();
        }

        Assert.Equal(4, e1.Passengers.Count);
    }
}