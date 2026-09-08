using MediatR;
using TelegramAi.Backend.Application.Features.Content.Create;
using TelegramAi.Backend.Domain.Content;
using TelegramAi.Backend.Application.Shared.Common.Text;

namespace TelegramAi.Backend.Application.Features.Telegram.Process;

public sealed record ProcessTelegramMessageRequest(ProcessTelegramMessageCommand Command)
    : IRequest<ProcessTelegramMessageResult>;

public sealed class ProcessTelegramMessageRequestHandler(IContentCreationWorkflow workflow)
    : IRequestHandler<ProcessTelegramMessageRequest, ProcessTelegramMessageResult>
{
    public async Task<ProcessTelegramMessageResult> Handle(
        ProcessTelegramMessageRequest request,
        CancellationToken cancellationToken)
    {
        ContentSourceType? sourceType = UrlDetector.ContainsUrl(request.Command.Text) ? null : ContentSourceType.Telegram;
        var content = await workflow.ExecuteAsync(new CreateContentCommand(request.Command.Text, sourceType), cancellationToken);
        return new ProcessTelegramMessageResult(
            request.Command.ChatId,
            string.IsNullOrWhiteSpace(request.Command.SenderDisplayName) ? "telegram-user" : request.Command.SenderDisplayName.Trim(),
            DateTimeOffset.UtcNow,
            content);
    }

}
