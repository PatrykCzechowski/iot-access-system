using FluentValidation;

namespace AccessControl.Application.Zones.Commands;

public sealed class DeleteAccessZoneCommandValidator : AbstractValidator<DeleteAccessZoneCommand>
{
    public DeleteAccessZoneCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
