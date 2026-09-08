using TelegramAi.Backend.Application.Features.Telegram.Process;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Telegram.Formatting;

public interface ITelegramResponseFormatter
{
    string Format(ProcessTelegramMessageResult result);
    IReadOnlyList<string> FormatSearch(FindContentsQuery query, IReadOnlyList<ContentItem> contents);
    string FormatAnswer(SemanticAnswerResult result);
    IReadOnlyList<string> FormatAnswerSources(SemanticAnswerResult result);
}
