namespace ElevatorSim.Domain;

public sealed class Request
{
    public int Floor { get; }
    public Direction Direction { get; }
    public int People { get; set; }

    public Request(int floor, Direction direction, int people)
    {
        if (!Enum.IsDefined(typeof(Direction), direction) || direction == Direction.Idle)
            throw new ArgumentException("Direction must be Up or Down.", nameof(direction));
        if (people <= 0) throw new ArgumentOutOfRangeException(nameof(people), "People must be > 0.");

        Floor = floor;
        Direction = direction;
        People = people;
    }

    public override string ToString() => $"Req(F:{Floor}, Dir:{Direction}, P:{People})";
}