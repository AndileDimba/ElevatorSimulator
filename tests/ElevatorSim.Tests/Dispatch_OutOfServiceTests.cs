using ElevatorSim.Domain;
using ElevatorSim.Infrastructure;
using ElevatorSim.Infrastructure.Dispatch;
using ElevatorSim.Infrastructure.Elevators;
using Xunit;

namespace ElevatorSim.Tests
{
    public class Dispatch_OutOfServiceTests
    {
        [Fact]
        public void OutOfService_Skips_Closest_Elevator()
        {
            var e1 = new PassengerElevator("E1", startFloor: 0);
            var e2 = new PassengerElevator("E2", startFloor: 10);

            var building = new Building(12, new NearestAvailableDispatch());
            building.AddElevator(e1);
            building.AddElevator(e2);

            building.SetOutOfService("E1", true);

            var chosen = building.DispatchStrategy.Dispatch(building, 0, Direction.Up);

            Assert.NotNull(chosen);
            Assert.Equal("E2", chosen!.Id);
        }

        [Fact]
        public void OutOfService_ReEnabled_Elevator_Is_Chosen()
        {
            var e1 = new PassengerElevator("E1", startFloor: 0);
            var e2 = new PassengerElevator("E2", startFloor: 10);

            var building = new Building(12, new NearestAvailableDispatch());
            building.AddElevator(e1);
            building.AddElevator(e2);

            building.SetOutOfService("E1", true);
            building.SetOutOfService("E1", false);

            var chosen = building.DispatchStrategy.Dispatch(building, 0, Direction.Up);

            Assert.NotNull(chosen);
            Assert.Equal("E1", chosen!.Id);
        }
    }
}