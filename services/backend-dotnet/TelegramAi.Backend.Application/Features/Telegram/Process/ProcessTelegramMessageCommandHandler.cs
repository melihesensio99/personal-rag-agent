using MediatR;
using TelegramAi.Backend.Application.Features.Content.Create;
using TelegramAi.Backend.Application.Shared.Common.Text;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Telegram.Process;

public sealed class ProcessTelegramMessageCommandHandler(IContentCreationWorkflow workflow) : IRequestHandler<ProcessTelegramMessageCommand, ProcessTelegramMessageResult>
{
    public async Task<ProcessTelegramMessageResult> Handle(ProcessTelegramMessageCommand request, CancellationToken cancellationToken)
    {
        ContentSourceType? sourceType = UrlDetector.ContainsUrl(request.Text) ? null : ContentSourceType.Telegram;
        var content = await workflow.ExecuteAsync(new CreateContentCommand(request.Text, sourceType), cancellationToken);
        return new(
            request.ChatId,
            string.IsNullOrWhiteSpace(request.SenderDisplayName) ? "telegram-user" : request.SenderDisplayName.Trim(),
            DateTimeOffset.UtcNow,
            content);
    }
}
