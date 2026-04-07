using FluentValidation;

namespace AccessControl.Application.Cards.Commands;

public sealed class CreateAccessCardCommandValidator : AbstractValidator<CreateAccessCardCommand>
{
    public CreateAccessCardCommandValidator()
    {
        RuleFor(x => x.CardUid)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.CardholderId)
            .NotEqual(Guid.Empty)
            .When(x => x.CardholderId.HasValue);

        RuleFor(x => x.Label)
            .MaximumLength(200)
            .When(x => x.Label is not null);
    }
}
