using System;

namespace ElevatorSim.Domain
{
    public static class DispatchStrategyExtensions
    {
        public static IElevator? Dispatch(this IDispatchStrategy strategy, Building building, int floor, Direction direction)
        {
            if (strategy == null) throw new ArgumentNullException(nameof(strategy));
            if (building == null) throw new ArgumentNullException(nameof(building));
            var req = new Request(floor, direction, count: 1);
            return strategy.ChooseElevator(building, req);
        }
    }
}