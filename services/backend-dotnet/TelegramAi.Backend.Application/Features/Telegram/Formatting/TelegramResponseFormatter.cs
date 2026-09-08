using TelegramAi.Backend.Application.Features.Telegram.Process;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Telegram.Formatting;

public sealed class TelegramResponseFormatter(
    ITelegramMessageResponseFormatter message,
    ITelegramContentSearchResponseFormatter search,
    ITelegramSemanticAnswerResponseFormatter answer) : ITelegramResponseFormatter
{
    public string Format(ProcessTelegramMessageResult result) => message.Format(result);
    public IReadOnlyList<string> FormatSearch(FindContentsQuery query, IReadOnlyList<ContentItem> contents) => search.FormatMessages(query, contents);
    public string FormatAnswer(SemanticAnswerResult result) => answer.Format(result);
    public IReadOnlyList<string> FormatAnswerSources(SemanticAnswerResult result) => answer.FormatSourceMessages(result);
}
