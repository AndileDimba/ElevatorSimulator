using ElevatorSim.Domain;
using ElevatorSim.Infrastructure;
using ElevatorSim.Infrastructure.Dispatch;
using ElevatorSim.Infrastructure.Elevators;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ElevatorSim.Tests
{
    public class Metrics_WaitTimeTests
    {
        [Fact]
        public void Metrics_Increments_When_Passengers_Board()
        {
            var e1 = new PassengerElevator("E1", startFloor: 0, capacity: 3, speedTicksPerFloor: 1, doorOpenTicks: 1, doorCloseTicks: 1);
            var building = new Building(12, new NearestAvailableDispatch());
            building.AddElevator(e1);

            building.SubmitCall(new Request(0, Direction.Up, 3));

            for (int i = 0; i < 30; i++)
            {
                building.TickAll();
                var (served, _, _) = building.GetWaitMetrics();
                if (served >= 3) break;
            }

            var (servedFinal, avg, max) = building.GetWaitMetrics();
            Assert.Equal(3, servedFinal);
            Assert.True(avg >= 0.0);
            Assert.True(max >= 0);
        }

        [Fact]
        public void Metrics_Reset_Clears_Counters()
        {
            var e1 = new PassengerElevator("E1", startFloor: 0, capacity: 2, speedTicksPerFloor: 1, doorOpenTicks: 1, doorCloseTicks: 1);
            var building = new Building(12, new NearestAvailableDispatch());
            building.AddElevator(e1);

            building.SubmitCall(new Request(0, Direction.Up, 2));

            for (int i = 0; i < 30; i++)
            {
                building.TickAll();
                var (served, _, _) = building.GetWaitMetrics();
                if (served >= 2) break;
            }

            var (servedBefore, avgBefore, maxBefore) = building.GetWaitMetrics();
            Assert.Equal(2, servedBefore);
            Assert.True(avgBefore >= 0.0);
            Assert.True(maxBefore >= 0);

            building.ResetWaitMetrics();
            var (servedAfter, avgAfter, maxAfter) = building.GetWaitMetrics();

            Assert.Equal(0, servedAfter);
            Assert.Equal(0.0, avgAfter);
            Assert.Equal(0, maxAfter);
        }

        [Fact]
        public void Metrics_Counts_First_Boarding_In_Overflow_Scenario()
        {
            var e1 = new PassengerElevator("E1", startFloor: 0, capacity: 4, speedTicksPerFloor: 1, doorOpenTicks: 1, doorCloseTicks: 1);
            var building = new Building(12, new NearestAvailableDispatch());
            building.AddElevator(e1);

            building.SubmitCall(new Request(0, Direction.Up, 6));

            bool sawFirstStop = false;
            for (int i = 0; i < 50; i++)
            {
                building.TickAll();
                var recent = GetRecentEvents(building, 6);
                if (recent.Any(ev => ev.Contains("Stop F:0")))
                {
                    sawFirstStop = true;
                    break;
                }
            }

            Assert.True(sawFirstStop);

            var (servedAfterFirstStop, _, _) = building.GetWaitMetrics();
            Assert.Equal(4, servedAfterFirstStop);
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