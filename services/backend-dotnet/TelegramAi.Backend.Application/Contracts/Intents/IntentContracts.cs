namespace TelegramAi.Backend.Application.Contracts.Intents;
public sealed record ClassifyIntentInput(string Message, string CurrentDate);
public sealed record ClassifyIntentResult(string Action, string Intent, string? Query, string? Content, string? ContentKind, string? SourceType, string TimeFilter, string? DateFrom, string? DateTo, string? SemanticQuery, IReadOnlyList<string> Keywords, bool NeedsClarification, string? ClarificationMessage);
