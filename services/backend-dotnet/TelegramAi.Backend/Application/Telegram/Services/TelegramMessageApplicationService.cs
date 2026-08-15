using TelegramAi.Backend.Application.Content.Commands;
using TelegramAi.Backend.Application.Content.Services;
using TelegramAi.Backend.Application.Telegram.Classification;
using TelegramAi.Backend.Application.Telegram.Commands;
using TelegramAi.Backend.Application.Telegram.Results;

namespace TelegramAi.Backend.Application.Telegram.Services;

public sealed class TelegramMessageApplicationService(
    IContentApplicationService contentApplicationService,
    ITelegramContentSourceDetector contentSourceDetector) : ITelegramMessageApplicationService
{
    public async Task<ProcessTelegramMessageResult> ProcessAsync(
        ProcessTelegramMessageCommand command,
        CancellationToken cancellationToken)
    {
        var sourceType = contentSourceDetector.Detect(command.Text);

        var contentItem = await contentApplicationService.CreateAsync(
            new CreateContentCommand(
                Text: command.Text,
                SourceType: sourceType),
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
