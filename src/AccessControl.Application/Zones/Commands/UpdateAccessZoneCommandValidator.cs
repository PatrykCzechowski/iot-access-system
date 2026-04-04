using FluentValidation;

namespace AccessControl.Application.Zones.Commands;

public sealed class UpdateAccessZoneCommandValidator : AbstractValidator<UpdateAccessZoneCommand>
{
    public UpdateAccessZoneCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => x.Description is not null);
    }
}
