using ElevatorSim.Domain;

namespace ElevatorSim.Infrastructure.Dispatch;

public sealed class NearestAvailableDispatch : IDispatchStrategy
{
    public IElevator? ChooseElevator(Building building, Request request)
    {
        // Day 1: stub. Return first elevator (or null if none).
        return building.Elevators.FirstOrDefault();
    }
}