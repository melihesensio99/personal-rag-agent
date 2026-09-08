using TelegramAi.Backend.Application.Features.Telegram.Commands;
using TelegramAi.Backend.Application.Features.Telegram.Results;

namespace TelegramAi.Backend.Application.Features.Telegram.Services;

public interface ITelegramMessageApplicationService
{
    Task<ProcessTelegramMessageResult> ProcessAsync(
        ProcessTelegramMessageCommand command,
        CancellationToken cancellationToken);
}
