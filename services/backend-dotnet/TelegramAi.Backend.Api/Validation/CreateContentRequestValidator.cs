using FluentValidation;
using TelegramAi.Backend.Api.Contracts.Content;

namespace TelegramAi.Backend.Api.Validation;

public sealed class CreateContentRequestValidator : AbstractValidator<CreateContentRequest>
{
    public CreateContentRequestValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(200_000);
    }
}
