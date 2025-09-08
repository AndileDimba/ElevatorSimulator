using ElevatorSim.Domain;
using ElevatorSim.Infrastructure;
using ElevatorSim.Infrastructure.Dispatch;
using ElevatorSim.Infrastructure.Elevators;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ElevatorSim.Tests
{
    public class Events_LogTests
    {
        [Fact]
        public void Events_Contains_Stop_When_Boarding_Occurs()
        {
            var e1 = new PassengerElevator("E1", startFloor: 0, capacity: 4, speedTicksPerFloor: 1, doorOpenTicks: 1, doorCloseTicks: 1);
            var building = new Building(12, new NearestAvailableDispatch());
            building.AddElevator(e1);

            building.SubmitCall(new Request(0, Direction.Up, 1));

            bool sawStopAtZero = false;
            for (int i = 0; i < 30; i++)
            {
                building.TickAll();
                var recent = GetRecentEvents(building, 10);
                if (recent.Any(ev => ev.Contains("Stop F:0")))
                {
                    sawStopAtZero = true;
                    break;
                }
            }

            Assert.True(sawStopAtZero, "Expected a 'Stop F:0' event after submitting a call and ticking.");
        }

        private static IReadOnlyList<string> GetRecentEvents(Building building, int count)
        {
            var mi = typeof(Building).GetMethod("GetRecentEvents");
            if (mi != null)
            {
                var res = mi.Invoke(building, new object[] { count }) as IEnumerable<string>;
                return res?.ToList() ?? new List<string>();
            }
            return new List<string>();
        }
    }
}