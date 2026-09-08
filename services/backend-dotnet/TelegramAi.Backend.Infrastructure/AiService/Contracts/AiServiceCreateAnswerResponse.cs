using System.Text.Json.Serialization;

namespace TelegramAi.Backend.Infrastructure.AiService.Contracts;

public sealed record AiServiceCreateAnswerResponse(
    [property: JsonPropertyName("content_id")] string ContentId,
    [property: JsonPropertyName("answer")] string Answer,
    [property: JsonPropertyName("used_chunk_indexes")] IReadOnlyList<int> UsedChunkIndexes,
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("provider")] string Provider);
