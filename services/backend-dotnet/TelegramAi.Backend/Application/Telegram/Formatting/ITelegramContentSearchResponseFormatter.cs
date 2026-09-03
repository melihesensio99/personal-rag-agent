using TelegramAi.Backend.Application.Content.Queries;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Telegram.Formatting;

public interface ITelegramContentSearchResponseFormatter
{
    IReadOnlyList<string> FormatMessages(SearchContentsQuery query, IReadOnlyList<ContentItem> contents);
}
