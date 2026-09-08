using System.Text.Json.Serialization;

namespace TelegramAi.Backend.Infrastructure.AiService.Contracts;

public sealed record AiServiceCreateAnswerRequest(
    [property: JsonPropertyName("content_id")] string ContentId,
    [property: JsonPropertyName("question")] string Question,
    [property: JsonPropertyName("chunks")] IReadOnlyList<AiServiceAnswerChunkRequest> Chunks);
