namespace TelegramAi.Backend.Api.Contracts.Content;

public sealed record ContentResponse(
    Guid Id,
    string SourceType,
    string RawText,
    DateTimeOffset CreatedAtUtc,
    string? OriginalUrl,
    string? ImageUrl,
    IReadOnlyList<ReaderBlockResponse> ReaderBlocks,
    ContentSummaryResponse Summary);

public sealed record ReaderBlockResponse(string Type, string? Text, int? Level, string? Url, string? Caption);
