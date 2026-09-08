namespace TelegramAi.Backend.Api.Contracts.Embeddings;

public sealed record CreateEmbeddingsResponse(
    string ContentId,
    string Model,
    int Dimension,
    IReadOnlyList<TextEmbeddingResponse> Embeddings);
