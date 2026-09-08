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
        var candidates = await semanticSearch.SearchAsync(string.IsNullOrWhiteSpace(retrievalQuery) ? query : retrievalQuery.Trim(), Math.Max(maxResults, CandidateLimit), contentId, cancellationToken);
        var selection = await reranking.SelectAsync(query, candidates, maxResults, cancellationToken);
        if (selection.Sources.Count == 0)
            return new(query, "Kayıtlı kaynaklarımda bu soruya cevap verecek yeterli bilgi bulunamadı.", "backend", [], []);
        var answer = await aiServiceClient.CreateAnswerAsync(new CreateAnswerInput("semantic-answer-query", query, BuildChunks(selection.Sources)), cancellationToken);
        return new(query, answer.Answer, answer.Provider, answer.UsedChunkIndexes, selection.Sources);
    }

    public async Task<SemanticAnswerDebugResult> AnswerDebugAsync(string query, int maxResults, Guid? contentId, CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        var search = await semanticSearch.SearchDebugAsync(query, Math.Max(maxResults, CandidateLimit), contentId, cancellationToken);
        var selection = await reranking.SelectAsync(query, search.Results, maxResults, cancellationToken);
        var context = BuildChunks(selection.Sources);
        if (context.Count == 0)
            return new(query, search.EmbeddingModel, search.EmbeddingDimension, search.QueryEmbeddingPreview, "backend",
                "Kayıtlı kaynaklarımda bu soruya cevap verecek yeterli bilgi bulunamadı.", [], [], [], options.Value.MinimumRerankScore,
                selection.Candidates, new(0, 0, 0, timer.ElapsedMilliseconds));
        var answer = await aiServiceClient.CreateAnswerAsync(new CreateAnswerInput("semantic-answer-query", query, context), cancellationToken);
        return new(query, search.EmbeddingModel, search.EmbeddingDimension, search.QueryEmbeddingPreview, answer.Provider, answer.Answer,
            answer.UsedChunkIndexes, context, selection.Sources, options.Value.MinimumRerankScore, selection.Candidates, new(0, 0, 0, timer.ElapsedMilliseconds));
    }

    private static IReadOnlyList<AnswerChunkInput> BuildChunks(IReadOnlyList<SemanticSearchChunkResult> sources) =>
        sources.Select((source, index) => new AnswerChunkInput(index, source.ContentId.ToString("N"), source.ChunkId.ToString("N"), source.ContentTitle,
            source.ContentUrl, source.SourceType.ToString(), source.ContentKind.ToString(), source.ChunkIndex, source.ChunkText,
            source.Distance, Math.Max(0, 1 - source.Distance))).ToList();
}
