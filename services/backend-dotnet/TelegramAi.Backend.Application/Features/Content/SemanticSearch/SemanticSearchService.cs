using TelegramAi.Backend.Application.Contracts.Embeddings;
using TelegramAi.Backend.Application.Shared.Abstractions;

namespace TelegramAi.Backend.Application.Features.Content.SemanticSearch;

public sealed class SemanticSearchService(
    IAiServiceClient aiServiceClient,
    IContentRepository contentRepository) : ISemanticSearchService
{
    public async Task<IReadOnlyList<SemanticSearchChunkResult>> SearchAsync(
        string query, int maxResults, Guid? contentId, CancellationToken cancellationToken)
    {
        var result = await SearchDebugAsync(query, maxResults, contentId, cancellationToken);
        return result.Results;
    }

    public async Task<SemanticSearchDebugResult> SearchDebugAsync(
        string query, int maxResults, Guid? contentId, CancellationToken cancellationToken)
    {
        var embeddings = await aiServiceClient.CreateEmbeddingsAsync(
            new CreateEmbeddingsInput("semantic-search-query", [query]), cancellationToken);
        var queryEmbedding = embeddings.Embeddings.SingleOrDefault()?.Embedding
            ?? throw new InvalidOperationException("AI service did not return a query embedding.");
        var results = await contentRepository.SemanticSearchChunksAsync(
            new SemanticSearchChunksQuery(queryEmbedding, maxResults, contentId), cancellationToken);
        return new SemanticSearchDebugResult(query, embeddings.Model, embeddings.Dimension,
            queryEmbedding.Take(8).ToList(), results);
    }
}
