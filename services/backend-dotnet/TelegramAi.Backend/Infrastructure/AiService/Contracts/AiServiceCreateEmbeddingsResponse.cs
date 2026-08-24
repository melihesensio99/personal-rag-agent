using System.Text.Json.Serialization;

namespace TelegramAi.Backend.Infrastructure.AiService.Contracts;

public sealed record AiServiceCreateEmbeddingsResponse(
    [property: JsonPropertyName("content_id")] string ContentId,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("dimension")] int Dimension,
    [property: JsonPropertyName("embeddings")] IReadOnlyList<AiServiceTextEmbeddingResponse> Embeddings);
