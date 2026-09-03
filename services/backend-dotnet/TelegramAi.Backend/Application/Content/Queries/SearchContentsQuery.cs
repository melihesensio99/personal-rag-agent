using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Content.Queries;

public sealed record SearchContentsQuery(
    IReadOnlyList<string> Keywords,
    ContentKind? ContentKind,
    ContentSourceType? SourceType,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    string? SemanticQuery = null,
    int MaxResults = 5);
