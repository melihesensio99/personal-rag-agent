using FluentValidation;
using TelegramAi.Backend.Api.Contracts.Search;

namespace TelegramAi.Backend.Api.Validation;

public sealed class SemanticSearchRequestValidator : AbstractValidator<SemanticSearchRequest>
{
    public SemanticSearchRequestValidator() => RuleFor(x => x.Query).NotEmpty().MaximumLength(10_000);
}
