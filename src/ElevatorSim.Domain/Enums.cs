namespace ElevatorSim.Domain;

public enum Direction { Down = -1, Idle = 0, Up = 1 }
public enum ElevatorState { DoorsClosed, DoorsOpening, DoorsOpen, DoorsClosing }
public enum ElevatorType { Passenger, HighSpeed, Freight, Glass }