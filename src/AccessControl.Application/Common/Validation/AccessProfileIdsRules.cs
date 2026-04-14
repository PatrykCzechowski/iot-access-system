using FluentValidation;

namespace AccessControl.Application.Common.Validation;

public static class AccessProfileIdsRules
{
    public static void AddAccessProfileIdsRules<T>(
        this AbstractValidator<T> validator,
        Func<T, List<Guid>?> accessor)
    {
        validator.RuleForEach(x => accessor(x))
            .NotEqual(Guid.Empty)
            .OverridePropertyName("AccessProfileIds")
            .When(x => accessor(x) is not null);

        validator.RuleFor(x => accessor(x))
            .Must(ids => ids is null || ids.Count == ids.Distinct().Count())
            .OverridePropertyName("AccessProfileIds")
            .WithMessage("Duplicate access profile IDs are not allowed.");
    }
}
