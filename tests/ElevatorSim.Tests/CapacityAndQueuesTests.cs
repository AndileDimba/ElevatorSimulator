using ElevatorSim.Application;
using ElevatorSim.Domain;
using ElevatorSim.Infrastructure.Dispatch;
using ElevatorSim.Infrastructure.Elevators;
using System.Collections.Generic;
using Xunit;

public class CapacityAndQueuesTests
{
    [Fact]
    public void LoadsUpToCapacityAndLeavesRemainderQueued()
    {
        var e1 = new PassengerElevator("E1", startFloor: 0, capacity: 4, speedTicksPerFloor: 1, doorOpenTicks: 1, doorCloseTicks: 1);
        var building = new Building(12, new NearestAvailableDispatch());
        building.AddElevator(e1);

        var req = new Request(0, Direction.Up, 6);
        building.SubmitCall(req);
        e1.Assign(req);

        for (int i = 0; i < 3; i++)
        {
            building.TickAll();
        }

        Assert.Equal(4, e1.Passengers.Count);
    }

    [Fact]
    public void HighSpeedElevator_MovesFaster()
    {
        var elevator = new HighSpeedElevator("HS1");
        Assert.Equal(3, elevator.SpeedTicksPerFloor);
    }

    [Fact]
    public void Dispatch_PrefersHighSpeedForLongDistance()
    {
        var elevators = new IElevator[]
        {
        new PassengerElevator("E1", startFloor: 0),
        new HighSpeedElevator("E2", startFloor: 5)
        };

        var building = new ConfigureBuildingUseCase()
            .CreateDefault(12, new NearestAvailableDispatch(), elevators);

        var dispatched = building.DispatchStrategy.Dispatch(building, 10, Direction.Down);

        Assert.NotNull(dispatched);
        Assert.Equal("E2", dispatched!.Id);
    }
}