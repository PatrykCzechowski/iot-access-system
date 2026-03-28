using FluentValidation;

namespace AccessControl.Application.Devices.Commands;

public sealed class CreateDeviceCommandValidator : AbstractValidator<CreateDeviceCommand>
{
    public CreateDeviceCommandValidator()
    {
        RuleFor(x => x.HardwareId)
            .NotEqual(Guid.Empty).WithMessage("HardwareId cannot be empty.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.ZoneId)
            .NotEqual(Guid.Empty).WithMessage("ZoneId cannot be empty.");
    }
}