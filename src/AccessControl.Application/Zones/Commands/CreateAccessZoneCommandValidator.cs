using FluentValidation;

namespace AccessControl.Application.Zones.Commands;

public sealed class CreateAccessZoneCommandValidator : AbstractValidator<CreateAccessZoneCommand>
{
    public CreateAccessZoneCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => x.Description is not null);
    }
}
