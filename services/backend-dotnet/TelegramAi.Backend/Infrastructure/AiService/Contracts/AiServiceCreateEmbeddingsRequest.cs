using System.Text.Json.Serialization;

namespace TelegramAi.Backend.Infrastructure.AiService.Contracts;

public sealed record AiServiceCreateEmbeddingsRequest(
    [property: JsonPropertyName("content_id")] string ContentId,
    [property: JsonPropertyName("texts")] IReadOnlyList<string> Texts);
