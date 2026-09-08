namespace TelegramAi.Backend.Api.Contracts.Search;

public sealed record SemanticSearchResultResponse(
    Guid ContentId,
    Guid ChunkId,
    string ContentTitle,
    string ContentUrl,
    string SourceType,
    string ContentKind,
    int ChunkIndex,
    string ChunkText,
    double Distance,
    double Similarity,
    DateTimeOffset ContentCreatedAtUtc);
