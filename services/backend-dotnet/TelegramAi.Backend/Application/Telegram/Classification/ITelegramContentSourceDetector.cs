using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Telegram.Classification;

public interface ITelegramContentSourceDetector
{
    ContentSourceType Detect(string messageText);
}
