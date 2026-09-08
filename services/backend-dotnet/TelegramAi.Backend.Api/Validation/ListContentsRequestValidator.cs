using FluentValidation;
using TelegramAi.Backend.Api.Contracts.Content;

namespace TelegramAi.Backend.Api.Validation;

public sealed class ListContentsRequestValidator : AbstractValidator<ListContentsRequest>
{
    public ListContentsRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SourceType)
            .Must(value => string.IsNullOrWhiteSpace(value) || ContentSourceTypeParser.TryParse(value, out _))
            .WithMessage("Invalid sourceType.");
        RuleFor(x => x.ToUtc).GreaterThan(x => x.FromUtc)
            .When(x => x.FromUtc.HasValue && x.ToUtc.HasValue)
            .WithMessage("toUtc must be later than fromUtc.");
    }
}
