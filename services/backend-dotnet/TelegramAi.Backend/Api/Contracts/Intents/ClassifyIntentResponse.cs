namespace TelegramAi.Backend.Api.Contracts.Intents;

public sealed record ClassifyIntentResponse(
    string Intent,
    string? ContentKind,
    string? SourceType,
    string TimeFilter,
    IReadOnlyList<string> Keywords,
    bool NeedsClarification);
