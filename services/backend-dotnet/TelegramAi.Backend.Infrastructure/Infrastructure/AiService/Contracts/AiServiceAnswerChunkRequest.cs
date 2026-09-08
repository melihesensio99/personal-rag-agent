using System.Text.Json.Serialization;

namespace TelegramAi.Backend.Infrastructure.AiService.Contracts;

public sealed record AiServiceAnswerChunkRequest(
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("content_id")] string ContentId,
    [property: JsonPropertyName("chunk_id")] string ChunkId,
    [property: JsonPropertyName("content_title")] string ContentTitle,
    [property: JsonPropertyName("content_url")] string ContentUrl,
    [property: JsonPropertyName("source_type")] string SourceType,
    [property: JsonPropertyName("content_kind")] string ContentKind,
    [property: JsonPropertyName("chunk_index")] int ChunkIndex,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("distance")] double Distance,
    [property: JsonPropertyName("similarity")] double Similarity);
