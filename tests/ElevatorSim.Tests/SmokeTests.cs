using ElevatorSim.Application;
using ElevatorSim.Domain;
using ElevatorSim.Infrastructure.Dispatch;
using ElevatorSim.Infrastructure.Elevators;
using Xunit;

namespace ElevatorSim.Tests;

public class SmokeTests
{
    [Fact]
    public void Building_Creates_With_StubElevators()
    {
        var cfg = new ConfigureBuildingUseCase();
        var b = cfg.CreateDefault(
            floors: 10,
            new NearestAvailableDispatch(),
            new[] { new StubElevator("E1"), new StubElevator("E2", startFloor: 3) });

        Assert.Equal(10, b.Floors);
        Assert.Equal(2, b.Elevators.Count);
    }
}