namespace TelegramAi.Backend.Api.Contracts.Intents;

public sealed record ClassifyIntentResponse(
    string Action,
    string Intent,
    string? Query,
    string? Content,
    string? ContentKind,
    string? SourceType,
    string TimeFilter,
    string? DateFrom,
    string? DateTo,
    string? SemanticQuery,
    IReadOnlyList<string> Keywords,
    bool NeedsClarification,
    string? ClarificationMessage);
