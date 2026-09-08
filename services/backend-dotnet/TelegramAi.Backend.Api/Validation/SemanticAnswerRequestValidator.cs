using FluentValidation;
using TelegramAi.Backend.Api.Contracts.Answers;

namespace TelegramAi.Backend.Api.Validation;

public sealed class SemanticAnswerRequestValidator : AbstractValidator<SemanticAnswerRequest>
{
    public SemanticAnswerRequestValidator() => RuleFor(x => x.Query).NotEmpty().MaximumLength(10_000);
}
