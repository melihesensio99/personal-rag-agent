namespace TelegramAi.Backend.Application.Features.Content.Queries;

public sealed record SemanticSearchChunksQuery(
    IReadOnlyList<float> Embedding,
    int MaxResults = 5,
    Guid? ContentId = null);
