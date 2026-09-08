namespace TelegramAi.Backend.Application.Features.Telegram.Agents;

public sealed record IntentDecision(
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

public interface IIntentClassifier
{
    Task<IntentDecision> ClassifyAsync(string text, CancellationToken cancellationToken);
}
