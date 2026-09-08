namespace TelegramAi.Backend.Api.Contracts.Chunks;

public sealed record TextChunkResponse(
    int Index,
    string Text,
    int CharStart,
    int CharEnd);
