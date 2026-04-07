using FluentValidation;

namespace AccessControl.Application.AccessProfiles.Commands;

public sealed class CreateAccessProfileCommandValidator : AbstractValidator<CreateAccessProfileCommand>
{
    public CreateAccessProfileCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
