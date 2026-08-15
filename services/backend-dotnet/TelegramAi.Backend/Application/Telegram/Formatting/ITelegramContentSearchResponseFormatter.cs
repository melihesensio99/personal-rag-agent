using TelegramAi.Backend.Application.Content.Queries;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Telegram.Formatting;

public interface ITelegramContentSearchResponseFormatter
{
    string Format(SearchContentsQuery query, IReadOnlyList<ContentItem> contents);
}
