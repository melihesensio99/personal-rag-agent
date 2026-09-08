using TelegramAi.Backend.Application.Features.Telegram.Process;

namespace TelegramAi.Backend.Application.Features.Telegram.Services;

public interface ITelegramMessageApplicationService
{
    Task<ProcessTelegramMessageResult> ProcessAsync(
        ProcessTelegramMessageCommand command,
        CancellationToken cancellationToken);
}
