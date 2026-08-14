using TelegramAi.Backend.Application.Content.Commands;
using TelegramAi.Backend.Application.Content.Services;
using TelegramAi.Backend.Application.Telegram.Commands;
using TelegramAi.Backend.Application.Telegram.Results;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Telegram.Services;

public sealed class TelegramMessageApplicationService(
    IContentApplicationService contentApplicationService) : ITelegramMessageApplicationService
{
    public async Task<ProcessTelegramMessageResult> ProcessAsync(
        ProcessTelegramMessageCommand command,
        CancellationToken cancellationToken)
    {
        var contentItem = await contentApplicationService.CreateAsync(
            new CreateContentCommand(
                Text: command.Text,
                SourceType: ContentSourceType.Telegram),
            cancellationToken);

        return new ProcessTelegramMessageResult(
            ChatId: command.ChatId,
            SenderDisplayName: string.IsNullOrWhiteSpace(command.SenderDisplayName)
                ? "telegram-user"
                : command.SenderDisplayName.Trim(),
            ReceivedAtUtc: DateTimeOffset.UtcNow,
            Content: contentItem);
    }
}
