using System.Diagnostics;
using Microsoft.Extensions.Options;
using TelegramAi.Backend.Application.Contracts.Answers;
using TelegramAi.Backend.Application.Shared.Abstractions;
using TelegramAi.Backend.Application.Features.Content.SemanticSearch;

namespace TelegramAi.Backend.Application.Features.Content.SemanticAnswer;

public sealed class SemanticAnswerService(
    IAiServiceClient aiServiceClient,
    ISemanticSearchService semanticSearch,
    IRerankingService reranking,
    IOptions<AnswerRetrievalOptions> options) : ISemanticAnswerService
{
    private const int CandidateLimit = 20;

    public async Task<SemanticAnswerResult> AnswerAsync(string query, int maxResults, Guid? contentId, CancellationToken cancellationToken, string? retrievalQuery = null)
    {
        var context = await RetrieveAsync(query, maxResults, contentId, cancellationToken, retrievalQuery);
        var selection = context.Selection;
        if (selection.Sources.Count == 0)
            return new(query, "Kayıtlı kaynaklarımda bu soruya cevap verecek yeterli bilgi bulunamadı.", "backend", [], []);
        var answer = await aiServiceClient.CreateAnswerAsync(new CreateAnswerInput("semantic-answer-query", query, context.Chunks), cancellationToken);
        return new(query, answer.Answer, answer.Provider, NormalizeUsedIndexes(answer.UsedChunkIndexes, context.Chunks.Count), selection.Sources);
    }

    public async Task<SemanticAnswerDebugResult> AnswerDebugAsync(string query, int maxResults, Guid? contentId, CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        var context = await RetrieveAsync(query, maxResults, contentId, cancellationToken);
        var selection = context.Selection;
        if (context.Chunks.Count == 0)
            return new(query, context.Search.EmbeddingModel, context.Search.EmbeddingDimension, context.Search.QueryEmbeddingPreview, "backend",
                "Kayıtlı kaynaklarımda bu soruya cevap verecek yeterli bilgi bulunamadı.", [], [], [], options.Value.MinimumRerankScore,
                selection.Candidates, new(0, 0, 0, timer.ElapsedMilliseconds));
        var answer = await aiServiceClient.CreateAnswerAsync(new CreateAnswerInput("semantic-answer-query", query, context.Chunks), cancellationToken);
        return new(query, context.Search.EmbeddingModel, context.Search.EmbeddingDimension, context.Search.QueryEmbeddingPreview, answer.Provider, answer.Answer,
            NormalizeUsedIndexes(answer.UsedChunkIndexes, context.Chunks.Count), context.Chunks, selection.Sources, options.Value.MinimumRerankScore, selection.Candidates, new(0, 0, 0, timer.ElapsedMilliseconds));
    }

    private async Task<RagContext> RetrieveAsync(string query, int maxResults, Guid? contentId, CancellationToken cancellationToken, string? retrievalQuery = null)
    {
        var search = await semanticSearch.SearchDebugAsync(
            string.IsNullOrWhiteSpace(retrievalQuery) ? query : retrievalQuery.Trim(),
            Math.Max(maxResults, CandidateLimit), contentId, cancellationToken);
        var selection = await reranking.SelectAsync(query, search.Results, maxResults, cancellationToken);
        return new(search, selection, BuildChunks(selection.Sources));
    }

    private static IReadOnlyList<AnswerChunkInput> BuildChunks(IReadOnlyList<SemanticSearchChunkResult> sources) =>
        sources.Select((source, index) => new AnswerChunkInput(index, source.ContentId.ToString("N"), source.ChunkId.ToString("N"), source.ContentTitle,
            source.ContentUrl, source.SourceType.ToString(), source.ContentKind.ToString(), source.ChunkIndex, source.ChunkText,
            source.Distance, Math.Max(0, 1 - source.Distance))).ToList();

    private static IReadOnlyList<int> NormalizeUsedIndexes(IReadOnlyList<int> indexes, int contextCount) =>
        indexes.Where(index => index >= 0 && index < contextCount).Distinct().OrderBy(index => index).ToList();

    private sealed record RagContext(
        SemanticSearchDebugResult Search,
        RerankingSelectionResult Selection,
        IReadOnlyList<AnswerChunkInput> Chunks);
}
