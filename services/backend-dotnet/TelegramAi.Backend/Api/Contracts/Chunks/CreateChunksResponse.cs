namespace TelegramAi.Backend.Api.Contracts.Chunks;

public sealed record CreateChunksResponse(
    string ContentId,
    int ChunkSize,
    int Overlap,
    int TotalChunks,
    IReadOnlyList<TextChunkResponse> Chunks);
