namespace ElevatorSim.Domain;

public sealed class Passenger
{
    public int OriginFloor { get; }
    public int DestinationFloor { get; }
    public Direction Direction => DestinationFloor > OriginFloor ? Direction.Up
                               : DestinationFloor < OriginFloor ? Direction.Down
                               : Direction.Idle;

    public Passenger(int originFloor, int destinationFloor)
    {
        OriginFloor = originFloor;
        DestinationFloor = destinationFloor;
    }
}