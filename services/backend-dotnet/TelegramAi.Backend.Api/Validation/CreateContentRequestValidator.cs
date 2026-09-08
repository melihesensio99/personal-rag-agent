using FluentValidation;
using TelegramAi.Backend.Api.Contracts.Content;
using TelegramAi.Backend.Api.Validation;

namespace TelegramAi.Backend.Api.Validation;

public sealed class CreateContentRequestValidator : AbstractValidator<CreateContentRequest>
{
    public CreateContentRequestValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(200_000);
        RuleFor(x => x.SourceType)
            .Must(value => string.IsNullOrWhiteSpace(value) || ContentSourceTypeParser.TryParse(value, out _))
            .WithMessage("Invalid sourceType.");
    }
}
