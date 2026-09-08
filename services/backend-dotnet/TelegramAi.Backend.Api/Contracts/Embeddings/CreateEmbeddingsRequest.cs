namespace TelegramAi.Backend.Api.Contracts.Embeddings;

public sealed record CreateEmbeddingsRequest(
    string ContentId,
    IReadOnlyList<string> Texts);
