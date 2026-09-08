namespace TelegramAi.Backend.Application.Features.Content.Queries;

public sealed record SemanticSearchDebugResult(
    string Query,
    string EmbeddingModel,
    int EmbeddingDimension,
    IReadOnlyList<float> QueryEmbeddingPreview,
    IReadOnlyList<SemanticSearchChunkResult> Results);
