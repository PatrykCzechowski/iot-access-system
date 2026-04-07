using FluentValidation;

namespace AccessControl.Application.Cards.Commands;

public sealed class UpdateAccessCardCommandValidator : AbstractValidator<UpdateAccessCardCommand>
{
    public UpdateAccessCardCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.CardholderId)
            .NotEqual(Guid.Empty)
            .When(x => x.CardholderId.HasValue);

        RuleFor(x => x.Label)
            .MaximumLength(200)
            .When(x => x.Label is not null);
    }
}
