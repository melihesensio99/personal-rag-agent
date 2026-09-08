using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Content.Queries;

public sealed record ListContentsQuery(
    string? Search,
    ContentSourceType? SourceType,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    int Page,
    int PageSize);
