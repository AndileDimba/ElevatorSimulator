using ElevatorSim.Domain;
using FluentValidation;

public class RequestValidator : AbstractValidator<Request>
{
    public RequestValidator(int topFloor)
    {
        RuleFor(r => r.Floor).InclusiveBetween(0, topFloor - 1).WithMessage("Invalid floor.");
        RuleFor(r => r.Count).GreaterThan(0).WithMessage("Count must be positive.");
    }
}