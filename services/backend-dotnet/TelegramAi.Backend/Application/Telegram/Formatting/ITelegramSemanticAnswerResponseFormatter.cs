using TelegramAi.Backend.Application.Content.Queries;

namespace TelegramAi.Backend.Application.Telegram.Formatting;

public interface ITelegramSemanticAnswerResponseFormatter
{
    string Format(SemanticAnswerResult result);
}
