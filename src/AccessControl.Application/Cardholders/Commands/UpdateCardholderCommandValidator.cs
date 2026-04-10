using AccessControl.Application.Common.Validation;
using FluentValidation;

namespace AccessControl.Application.Cardholders.Commands;

public sealed class UpdateCardholderCommandValidator : AbstractValidator<UpdateCardholderCommand>
{
    public UpdateCardholderCommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).MaximumLength(100).EmailAddress().When(x => x.Email is not null);
        RuleFor(x => x.PhoneNumber).MaximumLength(20).When(x => x.PhoneNumber is not null);
        this.AddAccessProfileIdsRules(x => x.AccessProfileIds);
    }
}
