using FluentValidation;
using TelegramAi.Backend.Api.Contracts.Summaries;

namespace TelegramAi.Backend.Api.Validation;

public sealed class CreateSummaryRequestValidator : AbstractValidator<CreateSummaryRequest>
{
    public CreateSummaryRequestValidator() => RuleFor(x => x.Text).NotEmpty().MaximumLength(200_000);
}
