namespace TelegramAi.Backend.Application.Features.Content.SemanticSearch;

public sealed record SemanticSearchChunksQuery(
    IReadOnlyList<float> Embedding,
    int MaxResults = 5,
    Guid? ContentId = null);
