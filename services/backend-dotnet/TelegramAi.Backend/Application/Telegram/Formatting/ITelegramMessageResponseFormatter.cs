using TelegramAi.Backend.Application.Telegram.Results;

namespace TelegramAi.Backend.Application.Telegram.Formatting;

public interface ITelegramMessageResponseFormatter
{
    string Format(ProcessTelegramMessageResult result);
}
