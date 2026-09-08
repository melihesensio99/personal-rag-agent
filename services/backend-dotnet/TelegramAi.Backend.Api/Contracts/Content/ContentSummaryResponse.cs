namespace TelegramAi.Backend.Api.Contracts.Content;

public sealed record ContentSummaryResponse(
    string Title,
    string ShortSummary,
    IReadOnlyList<string> KeyPoints,
    IReadOnlyList<string> Tags,
    string Language,
    string Provider);
