namespace TelegramAi.Backend.Api.Contracts.Summaries;

public sealed record CreateSummaryRequest(
    string ContentId,
    string Text);
