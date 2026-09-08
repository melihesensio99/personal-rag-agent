using TelegramAi.Backend.Application.Shared.Abstractions;

using TelegramAi.Backend.Domain.Content;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using TelegramAi.Backend.Application.Shared.Common.Pagination;
using TelegramAi.Backend.Application.Contracts.Answers;

namespace TelegramAi.Backend.Application.Features.Content.Services;

public sealed class ContentApplicationService(
    IAiServiceClient aiServiceClient,
    IContentRepository contentRepository,
    ILogger<ContentApplicationService> logger,
    IOptions<AnswerRetrievalOptions> retrievalOptions,
    IContentCreationWorkflow creationWorkflow,
    IRerankingService rerankingService,
    ISemanticSearchService semanticSearchService,
    ISemanticAnswerService semanticAnswerService) : IContentApplicationService
{
    private const int SemanticCandidateLimit = 20;

    public async Task<ContentItem> CreateAsync(
        CreateContentCommand command,
        CancellationToken cancellationToken)
        => await creationWorkflow.ExecuteAsync(command, cancellationToken);

    public Task<ContentItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return contentRepository.GetByIdAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<ContentChunk>> GetChunksByContentIdAsync(
        Guid contentId,
        CancellationToken cancellationToken)
    {
        return contentRepository.GetChunksByContentIdAsync(contentId, cancellationToken);
    }

    public Task<IReadOnlyList<ContentItem>> SearchAsync(
        SearchContentsQuery query,
        CancellationToken cancellationToken)
    {
        return contentRepository.SearchAsync(query, cancellationToken);
    }

    public Task<PagedResult<ContentItem>> ListAsync(ListContentsQuery query, CancellationToken cancellationToken) =>
        contentRepository.ListAsync(query, cancellationToken);

    public Task<IReadOnlyList<SemanticSearchChunkResult>> SemanticSearchChunksAsync(
        string query,
        int maxResults,
        Guid? contentId,
        CancellationToken cancellationToken) =>
        semanticSearchService.SearchAsync(query, maxResults, contentId, cancellationToken);

    public async Task<SemanticSearchDebugResult> SemanticSearchChunksDebugAsync(
        string query,
        int maxResults,
        Guid? contentId,
        CancellationToken cancellationToken)
        => await semanticSearchService.SearchDebugAsync(query, maxResults, contentId, cancellationToken);

    public async Task<SemanticAnswerResult> SemanticAnswerAsync(
        string query,
        int maxResults,
        Guid? contentId,
        CancellationToken cancellationToken,
        string? retrievalQuery = null)
    {
        if (semanticAnswerService is not null)
            return await semanticAnswerService.AnswerAsync(query, maxResults, contentId, cancellationToken, retrievalQuery);

        var totalTimer = Stopwatch.StartNew();
        var stageTimer = Stopwatch.StartNew();
        var candidates = await SemanticSearchChunksAsync(
            string.IsNullOrWhiteSpace(retrievalQuery) ? query : retrievalQuery.Trim(),
            Math.Max(maxResults, SemanticCandidateLimit),
            contentId,
            cancellationToken);
        var retrievalMilliseconds = stageTimer.ElapsedMilliseconds;

        stageTimer.Restart();
        var selection = await rerankingService.SelectAsync(
            query,
            candidates,
            maxResults,
            cancellationToken);
        var rerankMilliseconds = stageTimer.ElapsedMilliseconds;
        var sources = selection.Sources;

        if (sources.Count == 0)
        {
            totalTimer.Stop();
            LogAnswerPipeline(
                query,
                candidates.Count,
                sources.Count,
                retrievalMilliseconds,
                rerankMilliseconds,
                0,
                totalTimer.ElapsedMilliseconds);

            return new SemanticAnswerResult(
                Query: query,
                Answer: "Kayıtlı kaynaklarımda bu soruya cevap verecek yeterli bilgi bulunamadı.",
                Provider: "backend",
                UsedChunkIndexes: [],
                Sources: []);
        }

        stageTimer.Restart();
        var answer = await aiServiceClient.CreateAnswerAsync(
            new CreateAnswerInput(
                ContentId: "semantic-answer-query",
                Question: query,
                Chunks: BuildAnswerChunks(sources)),
            cancellationToken);
        var answerMilliseconds = stageTimer.ElapsedMilliseconds;
        totalTimer.Stop();

        LogAnswerPipeline(
            query,
            candidates.Count,
            sources.Count,
            retrievalMilliseconds,
            rerankMilliseconds,
            answerMilliseconds,
            totalTimer.ElapsedMilliseconds);

        return new SemanticAnswerResult(
            Query: query,
            Answer: answer.Answer,
            Provider: answer.Provider,
            UsedChunkIndexes: answer.UsedChunkIndexes,
            Sources: sources);
    }

    public async Task<SemanticAnswerDebugResult> SemanticAnswerDebugAsync(
        string query,
        int maxResults,
        Guid? contentId,
        CancellationToken cancellationToken)
    {
        if (semanticAnswerService is not null)
            return await semanticAnswerService.AnswerDebugAsync(query, maxResults, contentId, cancellationToken);

        var totalTimer = Stopwatch.StartNew();
        var stageTimer = Stopwatch.StartNew();
        var searchDebug = await SemanticSearchChunksDebugAsync(
            query,
            Math.Max(maxResults, SemanticCandidateLimit),
            contentId,
            cancellationToken);
        var retrievalMilliseconds = stageTimer.ElapsedMilliseconds;

        stageTimer.Restart();
        var selection = await rerankingService.SelectAsync(
            query,
            searchDebug.Results,
            maxResults,
            cancellationToken);
        var rerankMilliseconds = stageTimer.ElapsedMilliseconds;
        var selectedSources = selection.Sources;
        var contextChunks = BuildAnswerChunks(selectedSources);

        if (contextChunks.Count == 0)
        {
            totalTimer.Stop();
            return new SemanticAnswerDebugResult(
                Query: query,
                EmbeddingModel: searchDebug.EmbeddingModel,
                EmbeddingDimension: searchDebug.EmbeddingDimension,
                QueryEmbeddingPreview: searchDebug.QueryEmbeddingPreview,
                AnswerProvider: "backend",
                Answer: "Kayıtlı kaynaklarımda bu soruya cevap verecek yeterli bilgi bulunamadı.",
                UsedChunkIndexes: [],
                ContextChunksSentToLlm: [],
                Sources: [],
                MinimumRerankScore: retrievalOptions.Value.MinimumRerankScore,
                RerankCandidates: selection.Candidates,
                Timing: new RagPipelineTiming(
                    retrievalMilliseconds,
                    rerankMilliseconds,
                    0,
                    totalTimer.ElapsedMilliseconds));
        }

        stageTimer.Restart();
        var answer = await aiServiceClient.CreateAnswerAsync(
            new CreateAnswerInput(
                ContentId: "semantic-answer-query",
                Question: query,
                Chunks: contextChunks),
            cancellationToken);
        var answerMilliseconds = stageTimer.ElapsedMilliseconds;
        totalTimer.Stop();

        return new SemanticAnswerDebugResult(
            Query: query,
            EmbeddingModel: searchDebug.EmbeddingModel,
            EmbeddingDimension: searchDebug.EmbeddingDimension,
            QueryEmbeddingPreview: searchDebug.QueryEmbeddingPreview,
            AnswerProvider: answer.Provider,
            Answer: answer.Answer,
            UsedChunkIndexes: answer.UsedChunkIndexes,
            ContextChunksSentToLlm: contextChunks,
            Sources: selectedSources,
            MinimumRerankScore: retrievalOptions.Value.MinimumRerankScore,
            RerankCandidates: selection.Candidates,
            Timing: new RagPipelineTiming(
                retrievalMilliseconds,
                rerankMilliseconds,
                answerMilliseconds,
                totalTimer.ElapsedMilliseconds));
    }

    private static IReadOnlyList<AnswerChunkInput> BuildAnswerChunks(
        IReadOnlyList<SemanticSearchChunkResult> sources)
    {
        return sources.Select((source, index) => new AnswerChunkInput(
            Index: index,
            ContentId: source.ContentId.ToString("N"),
            ChunkId: source.ChunkId.ToString("N"),
            ContentTitle: source.ContentTitle,
            ContentUrl: source.ContentUrl,
            SourceType: source.SourceType.ToString(),
            ContentKind: source.ContentKind.ToString(),
            ChunkIndex: source.ChunkIndex,
            Text: source.ChunkText,
            Distance: source.Distance,
            Similarity: Math.Max(0, 1 - source.Distance))).ToList();
    }

    private void LogAnswerPipeline(
        string query,
        int candidateCount,
        int selectedCount,
        long retrievalMilliseconds,
        long rerankMilliseconds,
        long answerMilliseconds,
        long totalMilliseconds)
    {
        logger.LogInformation(
            "RAG answer completed. Query={Query} Candidates={CandidateCount} Selected={SelectedCount} " +
            "RetrievalMs={RetrievalMs} RerankMs={RerankMs} AnswerMs={AnswerMs} TotalMs={TotalMs}",
            query,
            candidateCount,
            selectedCount,
            retrievalMilliseconds,
            rerankMilliseconds,
            answerMilliseconds,
            totalMilliseconds);
    }

}
