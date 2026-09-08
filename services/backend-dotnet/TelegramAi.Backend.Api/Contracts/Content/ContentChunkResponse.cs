namespace TelegramAi.Backend.Api.Contracts.Content;

public sealed record ContentChunkResponse(
    Guid Id,
    Guid ContentItemId,
    int Index,
    string Text,
    int CharStart,
    int CharEnd,
    bool HasEmbedding,
    DateTimeOffset CreatedAtUtc);
