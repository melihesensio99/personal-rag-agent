namespace TelegramAi.Backend.Application.Features.Content.SemanticAnswer;

public sealed record SemanticAnswerResult(
    string Query,
    string Answer,
    string Provider,
    IReadOnlyList<int> UsedChunkIndexes,
    IReadOnlyList<SemanticSearchChunkResult> Sources);
