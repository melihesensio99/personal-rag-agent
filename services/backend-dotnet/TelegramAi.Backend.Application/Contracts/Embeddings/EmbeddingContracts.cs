namespace TelegramAi.Backend.Application.Contracts.Embeddings;
public sealed record CreateEmbeddingsInput(string ContentId, IReadOnlyList<string> Texts);
public sealed record TextEmbeddingResult(int Index, IReadOnlyList<float> Embedding);
public sealed record CreateEmbeddingsResult(string ContentId, string Model, int Dimension, IReadOnlyList<TextEmbeddingResult> Embeddings);
