using ElevatorSim.Domain;
using ElevatorSim.Infrastructure.Elevators;

namespace ElevatorSim.Infrastructure.Dispatch;

public sealed class NearestAvailableDispatch : IDispatchStrategy
{
    public IElevator? ChooseElevator(Building building, Request request)
    {
        // Score elevators:
        // - Prefer those moving toward the request with matching direction and path
        // - Then idle elevators by distance
        // - Penalize those moving away
        // - Add small penalty for passenger load
        IElevator? best = null;
        int bestScore = int.MaxValue;

        foreach (var e in building.Elevators)
        {
            if (building.IsOutOfService(e.Id)) continue;
            var distance = Math.Abs(e.CurrentFloor - request.Floor);
            var loadPenalty = e.PassengerCount; // simple load penalty
            var directionPenalty = 0;

            if (e.Direction == Direction.Idle)
            {
                directionPenalty += 2; // slightly worse than perfectly aligned movers
            }
            else
            {
                bool movingToward = (e.Direction == Direction.Up && e.CurrentFloor <= request.Floor && request.Direction == Direction.Up)
                                    || (e.Direction == Direction.Down && e.CurrentFloor >= request.Floor && request.Direction == Direction.Down);
                if (!movingToward)
                    directionPenalty += 10; // moving away
            }

            int score = distance * 5 + directionPenalty + loadPenalty;

            if (score < bestScore)
            {
                bestScore = score;
                best = e;
            }
        }

        return best;
    }
}