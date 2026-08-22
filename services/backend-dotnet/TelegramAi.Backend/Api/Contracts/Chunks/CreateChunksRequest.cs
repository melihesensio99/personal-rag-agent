namespace TelegramAi.Backend.Api.Contracts.Chunks;

public sealed record CreateChunksRequest(
    string ContentId,
    string Text,
    int ChunkSize = 1200,
    int Overlap = 150);
