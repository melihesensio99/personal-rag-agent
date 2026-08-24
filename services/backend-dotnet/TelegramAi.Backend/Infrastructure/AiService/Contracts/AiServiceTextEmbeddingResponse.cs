using System.Text.Json.Serialization;

namespace TelegramAi.Backend.Infrastructure.AiService.Contracts;

public sealed record AiServiceTextEmbeddingResponse(
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("embedding")] IReadOnlyList<float> Embedding);
