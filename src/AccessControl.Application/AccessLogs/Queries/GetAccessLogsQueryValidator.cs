using FluentValidation;

namespace AccessControl.Application.AccessLogs.Queries;

public sealed class GetAccessLogsQueryValidator : AbstractValidator<GetAccessLogsQuery>
{
    public GetAccessLogsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .When(x => x.From.HasValue && x.To.HasValue)
            .WithMessage("'To' must be greater than or equal to 'From'.");
    }
}
