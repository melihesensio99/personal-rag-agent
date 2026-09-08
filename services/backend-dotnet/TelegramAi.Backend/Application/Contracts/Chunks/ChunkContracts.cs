namespace TelegramAi.Backend.Application.Contracts.Chunks;
public sealed record CreateChunksInput(string ContentId, string Text, int ChunkSize = 1200, int Overlap = 150);
public sealed record TextChunkResult(int Index, string Text, int CharStart, int CharEnd);
public sealed record CreateChunksResult(string ContentId, int ChunkSize, int Overlap, int TotalChunks, IReadOnlyList<TextChunkResult> Chunks);
