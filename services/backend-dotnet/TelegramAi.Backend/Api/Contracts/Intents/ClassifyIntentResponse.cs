namespace TelegramAi.Backend.Api.Contracts.Intents;

public sealed record ClassifyIntentResponse(
    string Action,
    string Intent,
    string? Query,
    string? Content,
    string? ContentKind,
    string? SourceType,
    string TimeFilter,
    IReadOnlyList<string> Keywords,
    bool NeedsClarification,
    string? ClarificationMessage);
