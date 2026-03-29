using FluentValidation;

namespace AccessControl.Application.Devices.Commands;

public sealed class UpdateDeviceConfigCommandValidator : AbstractValidator<UpdateDeviceConfigCommand>
{
    public UpdateDeviceConfigCommandValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.Settings)
            .NotEmpty()
            .WithMessage("At least one configuration setting is required.");
    }
}
