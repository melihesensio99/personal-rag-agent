namespace TelegramAi.Backend.Application.Content.Queries;

public sealed record SemanticSearchChunksQuery(
    IReadOnlyList<float> Embedding,
    int MaxResults = 5,
    Guid? ContentId = null);
