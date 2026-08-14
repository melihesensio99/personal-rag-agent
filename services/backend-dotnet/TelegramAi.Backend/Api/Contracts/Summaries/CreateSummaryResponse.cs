namespace TelegramAi.Backend.Api.Contracts.Summaries;

public sealed record CreateSummaryResponse(
    string ContentId,
    string Title,
    string ShortSummary,
    IReadOnlyList<string> KeyPoints,
    IReadOnlyList<string> Tags,
    string Language,
    string Provider);
