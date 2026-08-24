using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Content.Queries;

public sealed record SemanticSearchChunkResult(
    Guid ContentId,
    Guid ChunkId,
    string ContentTitle,
    string ContentUrl,
    ContentSourceType SourceType,
    ContentKind ContentKind,
    int ChunkIndex,
    string ChunkText,
    double Distance,
    DateTimeOffset ContentCreatedAtUtc);
