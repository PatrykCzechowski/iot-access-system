using FluentValidation;

namespace AccessControl.Application.AccessProfiles.Commands;

public sealed class UpdateAccessProfileCommandValidator : AbstractValidator<UpdateAccessProfileCommand>
{
    public UpdateAccessProfileCommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
