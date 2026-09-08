namespace TelegramAi.Backend.Api.Contracts.Embeddings;

public sealed record TextEmbeddingResponse(
    int Index,
    IReadOnlyList<float> Embedding);
