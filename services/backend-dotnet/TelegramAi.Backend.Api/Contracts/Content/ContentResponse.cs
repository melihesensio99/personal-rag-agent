namespace TelegramAi.Backend.Api.Contracts.Content;

public sealed record ContentResponse(
    Guid Id,
    string SourceType,
    string RawText,
    DateTimeOffset CreatedAtUtc,
    ContentSummaryResponse Summary);
