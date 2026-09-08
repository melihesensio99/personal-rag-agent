using TelegramAi.Backend.Application.Features.Telegram.Process;

namespace TelegramAi.Backend.Application.Features.Telegram.Formatting;

public interface ITelegramMessageResponseFormatter
{
    string Format(ProcessTelegramMessageResult result);
}
