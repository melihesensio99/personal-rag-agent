namespace TelegramAi.Backend.Api.Contracts.Search;

public sealed record SemanticSearchDebugResponse(
    string Query,
    SemanticEmbeddingDebugResponse QueryEmbedding,
    IReadOnlyList<SemanticSearchResultResponse> Results);

public sealed record SemanticEmbeddingDebugResponse(
    string Model,
    int Dimension,
    IReadOnlyList<float> Preview);
