namespace TelegramAi.Backend.Application.Contracts.Summaries;

public sealed record CreateSummaryInput(string ContentId, string Text);
public sealed record CreateSummaryResult(string ContentId, string Title, string ShortSummary, IReadOnlyList<string> KeyPoints, IReadOnlyList<string> Tags, string Language, string Provider);
