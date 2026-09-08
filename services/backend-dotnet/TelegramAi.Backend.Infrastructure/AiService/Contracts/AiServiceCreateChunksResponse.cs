using System.Text.Json.Serialization;

namespace TelegramAi.Backend.Infrastructure.AiService.Contracts;

public sealed record AiServiceCreateChunksResponse(
    [property: JsonPropertyName("content_id")] string ContentId,
    [property: JsonPropertyName("chunk_size")] int ChunkSize,
    [property: JsonPropertyName("overlap")] int Overlap,
    [property: JsonPropertyName("total_chunks")] int TotalChunks,
    [property: JsonPropertyName("chunks")] IReadOnlyList<AiServiceTextChunkResponse> Chunks);
