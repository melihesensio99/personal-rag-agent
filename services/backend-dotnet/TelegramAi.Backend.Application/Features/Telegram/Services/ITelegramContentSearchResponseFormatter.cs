using TelegramAi.Backend.Application.Features.Content.Queries;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Telegram.Formatting;

public interface ITelegramContentSearchResponseFormatter
{
    IReadOnlyList<string> FormatMessages(SearchContentsQuery query, IReadOnlyList<ContentItem> contents);
}
