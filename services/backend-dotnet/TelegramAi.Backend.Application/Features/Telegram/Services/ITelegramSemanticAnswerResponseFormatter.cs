

namespace TelegramAi.Backend.Application.Features.Telegram.Formatting;

public interface ITelegramSemanticAnswerResponseFormatter
{
    string Format(SemanticAnswerResult result);

    IReadOnlyList<string> FormatSourceMessages(SemanticAnswerResult result);
}
