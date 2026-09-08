namespace TelegramAi.Backend.Application.Features.Content.Queries;

public sealed record SemanticAnswerResult(
    string Query,
    string Answer,
    string Provider,
    IReadOnlyList<int> UsedChunkIndexes,
    IReadOnlyList<SemanticSearchChunkResult> Sources);
