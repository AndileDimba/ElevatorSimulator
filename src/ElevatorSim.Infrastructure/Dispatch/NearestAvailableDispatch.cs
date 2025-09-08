using ElevatorSim.Domain;
using ElevatorSim.Infrastructure.Elevators;

namespace ElevatorSim.Infrastructure.Dispatch;

public sealed class NearestAvailableDispatch : IDispatchStrategy
{
    public IElevator? ChooseElevator(Building building, Request request)
    {
        IElevator? best = null;
        int bestScore = int.MaxValue;

        foreach (var e in building.Elevators)
        {
            if (building.IsOutOfService(e.Id)) continue;
            var distance = Math.Abs(e.CurrentFloor - request.Floor);
            var loadPenalty = e.PassengerCount;
            var directionPenalty = 0;

            if (e.Direction == Direction.Idle)
            {
                directionPenalty += 2;
            }
            else
            {
                bool movingToward = (e.Direction == Direction.Up && e.CurrentFloor <= request.Floor && request.Direction == Direction.Up)
                                    || (e.Direction == Direction.Down && e.CurrentFloor >= request.Floor && request.Direction == Direction.Down);
                if (!movingToward)
                    directionPenalty += 10;
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