using FluentValidation;

namespace AccessControl.Application.Devices.Commands;

public sealed class UpdateDeviceCommandValidator : AbstractValidator<UpdateDeviceCommand>
{
    public UpdateDeviceCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.ZoneId)
            .NotEqual(Guid.Empty).WithMessage("ZoneId cannot be empty.");
    }
}
