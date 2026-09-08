using TelegramAi.Backend.Application.Shared.Abstractions;
using TelegramAi.Backend.Application.Features.Content.Commands;
using TelegramAi.Backend.Application.Features.Content.Exceptions;
using TelegramAi.Backend.Application.Features.Content.Queries;
using TelegramAi.Backend.Domain.Content;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using TelegramAi.Backend.Application.Shared.Common.Pagination;
using TelegramAi.Backend.Application.Features.Content.Handlers;
using TelegramAi.Backend.Application.Features.Content.Policies;
using TelegramAi.Backend.Application.Contracts.Summaries;
using TelegramAi.Backend.Application.Contracts.Extractions;
using TelegramAi.Backend.Application.Contracts.Chunks;
using TelegramAi.Backend.Application.Contracts.Embeddings;
using TelegramAi.Backend.Application.Contracts.Answers;
using TelegramAi.Backend.Application.Contracts.Reranking;

namespace TelegramAi.Backend.Application.Features.Content.Services;

public sealed class ContentApplicationService(
    IAiServiceClient aiServiceClient,
    IContentRepository contentRepository,
    ILogger<ContentApplicationService> logger,
    IOptions<AnswerRetrievalOptions> retrievalOptions,
    IListContentsHandler listContentsHandler) : IContentApplicationService
{
    private const int SemanticCandidateLimit = 20;
    private const int MaxChunksPerContent = 3;
    private const int MaxAnswerChunks = 8;
    private const int MaxAnswerContextCharacters = 12_000;

    public async Task<ContentItem> CreateAsync(
        CreateContentCommand command,
        CancellationToken cancellationToken)
    {
        var contentId = Guid.NewGuid();
        var extraction = await TryExtractAsync(contentId, command, cancellationToken);
        EnsureExtractionIsSaveable(extraction);

        var summaryInputText = ContentInputPolicy.ResolveSummaryInputText(command, extraction);
        var chunkInputText = ContentInputPolicy.ResolveChunkInputText(command, extraction, summaryInputText);
        var contentKind = ContentInputPolicy.ResolveContentKind(command, extraction);
        var sourceType = ContentInputPolicy.ResolveSourceType(command, extraction);

        var summary = await aiServiceClient.CreateSummaryAsync(
            new CreateSummaryInput(
                ContentId: contentId.ToString("N"),
                Text: summaryInputText),
            cancellationToken);

        var contentItem = ContentItem.Create(
            id: contentId,
            sourceType: sourceType,
            contentKind: contentKind,
            rawText: command.Text,
            summary: ContentSummary.Create(
                title: summary.Title,
                shortSummary: summary.ShortSummary,
                keyPoints: summary.KeyPoints,
                tags: summary.Tags,
                language: summary.Language,
                provider: summary.Provider));

        await contentRepository.AddAsync(contentItem, cancellationToken);
        await TryCreateAndSaveChunksAsync(contentItem.Id, chunkInputText, cancellationToken);

        return contentItem;
    }

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

    public Task<PagedResult<ContentItem>> ListAsync(ListContentsQuery query, CancellationToken cancellationToken)
    {
        return listContentsHandler.HandleAsync(query, cancellationToken);
    }

    public async Task<IReadOnlyList<SemanticSearchChunkResult>> SemanticSearchChunksAsync(
        string query,
        int maxResults,
        Guid? contentId,
        CancellationToken cancellationToken)
    {
        var debugResult = await SemanticSearchChunksDebugAsync(
            query,
            maxResults,
            contentId,
            cancellationToken);

        return debugResult.Results;
    }

    public async Task<SemanticSearchDebugResult> SemanticSearchChunksDebugAsync(
        string query,
        int maxResults,
        Guid? contentId,
        CancellationToken cancellationToken)
    {
        var (embeddings, queryEmbedding) = await CreateQueryEmbeddingAsync(query, cancellationToken);

        var results = await contentRepository.SemanticSearchChunksAsync(
            new SemanticSearchChunksQuery(
                Embedding: queryEmbedding,
                MaxResults: maxResults,
                ContentId: contentId),
            cancellationToken);

        return new SemanticSearchDebugResult(
            Query: query,
            EmbeddingModel: embeddings.Model,
            EmbeddingDimension: embeddings.Dimension,
            QueryEmbeddingPreview: queryEmbedding.Take(8).ToList(),
            Results: results);
    }

    public async Task<SemanticAnswerResult> SemanticAnswerAsync(
        string query,
        int maxResults,
        Guid? contentId,
        CancellationToken cancellationToken,
        string? retrievalQuery = null)
    {
        var totalTimer = Stopwatch.StartNew();
        var stageTimer = Stopwatch.StartNew();
        var candidates = await SemanticSearchChunksAsync(
            string.IsNullOrWhiteSpace(retrievalQuery) ? query : retrievalQuery.Trim(),
            Math.Max(maxResults, SemanticCandidateLimit),
            contentId,
            cancellationToken);
        var retrievalMilliseconds = stageTimer.ElapsedMilliseconds;

        stageTimer.Restart();
        var selection = await RerankAndSelectAnswerSourcesAsync(
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
        var totalTimer = Stopwatch.StartNew();
        var stageTimer = Stopwatch.StartNew();
        var searchDebug = await SemanticSearchChunksDebugAsync(
            query,
            Math.Max(maxResults, SemanticCandidateLimit),
            contentId,
            cancellationToken);
        var retrievalMilliseconds = stageTimer.ElapsedMilliseconds;

        stageTimer.Restart();
        var selection = await RerankAndSelectAnswerSourcesAsync(
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

    private async Task<(CreateEmbeddingsResult Response, IReadOnlyList<float> QueryEmbedding)> CreateQueryEmbeddingAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var embeddings = await aiServiceClient.CreateEmbeddingsAsync(
            new CreateEmbeddingsInput(
                ContentId: "semantic-search-query",
                Texts: [query]),
            cancellationToken);

        var queryEmbedding = embeddings.Embeddings.SingleOrDefault()?.Embedding
            ?? throw new InvalidOperationException("AI service did not return a query embedding.");

        return (embeddings, queryEmbedding);
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

    private async Task<RerankSelectionResult> RerankAndSelectAnswerSourcesAsync(
        string query,
        IReadOnlyList<SemanticSearchChunkResult> candidates,
        int requestedMaxResults,
        CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
        {
            return new RerankSelectionResult([], []);
        }

        var rerankResponse = await aiServiceClient.RerankAsync(
            new RerankInput(
                query,
                candidates.Select((candidate, index) => new RerankDocumentInput(index, candidate.ChunkText)).ToList()),
            cancellationToken);

        var scoresByIndex = rerankResponse.Scores.ToDictionary(score => score.Index, score => score.Score);
        var minimumScore = retrievalOptions.Value.MinimumRerankScore;
        var scoredCandidates = candidates
            .Select((candidate, index) => new ScoredAnswerCandidate(
                CandidateIndex: index,
                Candidate: candidate,
                RerankScore: scoresByIndex.GetValueOrDefault(index),
                HasRerankScore: scoresByIndex.ContainsKey(index)))
            .ToList();

        var passedCandidates = scoredCandidates
            .Where(item => item.HasRerankScore && item.RerankScore >= minimumScore)
            .OrderByDescending(item => item.RerankScore)
            .ToList();
        var selectedSources = SelectAnswerSources(passedCandidates, requestedMaxResults);
        var selectedChunkIds = selectedSources.Select(source => source.ChunkId).ToHashSet();

        var diagnostics = scoredCandidates
            .Select(item => new RagRerankCandidateDiagnostic(
                CandidateIndex: item.CandidateIndex,
                ContentId: item.Candidate.ContentId,
                ChunkId: item.Candidate.ChunkId,
                ContentTitle: item.Candidate.ContentTitle,
                ChunkIndex: item.Candidate.ChunkIndex,
                Similarity: Math.Max(0, 1 - item.Candidate.Distance),
                RerankScore: item.RerankScore,
                Accepted: selectedChunkIds.Contains(item.Candidate.ChunkId),
                Decision: ResolveRerankDecision(item, minimumScore, selectedChunkIds)))
            .OrderByDescending(item => item.RerankScore)
            .ToList();

        return new RerankSelectionResult(selectedSources, diagnostics);
    }

    private static IReadOnlyList<SemanticSearchChunkResult> SelectAnswerSources(
        IReadOnlyList<ScoredAnswerCandidate> candidates,
        int requestedMaxResults)
    {
        var totalLimit = Math.Clamp(requestedMaxResults, 1, MaxAnswerChunks);
        var groups = candidates
            .GroupBy(candidate => candidate.Candidate.ContentId)
            .Select(group => group
                .OrderByDescending(candidate => candidate.RerankScore)
                .Take(MaxChunksPerContent)
                .ToList())
            .OrderByDescending(group => group[0].RerankScore)
            .ToList();

        var selected = new List<SemanticSearchChunkResult>();
        var totalCharacters = 0;

        for (var round = 0; round < MaxChunksPerContent && selected.Count < totalLimit; round++)
        {
            foreach (var group in groups)
            {
                if (round >= group.Count || selected.Count >= totalLimit)
                {
                    continue;
                }

                var candidate = group[round].Candidate;
                if (selected.Count > 0 && totalCharacters + candidate.ChunkText.Length > MaxAnswerContextCharacters)
                {
                    continue;
                }

                selected.Add(candidate);
                totalCharacters += candidate.ChunkText.Length;
            }
        }

        return selected;
    }

    private static string ResolveRerankDecision(
        ScoredAnswerCandidate candidate,
        double minimumScore,
        IReadOnlySet<Guid> selectedChunkIds)
    {
        if (!candidate.HasRerankScore)
        {
            return "missing_rerank_score";
        }

        if (candidate.RerankScore < minimumScore)
        {
            return "below_rerank_threshold";
        }

        return selectedChunkIds.Contains(candidate.Candidate.ChunkId)
            ? "selected_for_answer"
            : "excluded_by_context_limits";
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

    private sealed record ScoredAnswerCandidate(
        int CandidateIndex,
        SemanticSearchChunkResult Candidate,
        double RerankScore,
        bool HasRerankScore);

    private async Task<CreateExtractionResult?> TryExtractAsync(
        Guid contentId,
        CreateContentCommand command,
        CancellationToken cancellationToken)
    {
        var url = ContentInputPolicy.TryExtractUrl(command.Text);

        if (url is null || command.SourceType is ContentSourceType.Telegram or ContentSourceType.Manual)
        {
            return null;
        }

        try
        {
            return await aiServiceClient.CreateExtractionAsync(
                new CreateExtractionInput(
                    ContentId: contentId.ToString("N"),
                    SourceType: command.SourceType?.ToString().ToLowerInvariant(),
                    Url: url,
                    Text: command.Text),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Extraction failed for content {ContentId}. Falling back to raw input.",
                contentId);

            return null;
        }
    }

    private async Task TryCreateAndSaveChunksAsync(
        Guid contentId,
        string text,
        CancellationToken cancellationToken)
    {
        try
        {
            var chunks = await aiServiceClient.CreateChunksAsync(
                new CreateChunksInput(
                    ContentId: contentId.ToString("N"),
                    Text: text),
                cancellationToken);

            var embeddingsByChunkIndex = await TryCreateEmbeddingsByChunkIndexAsync(
                contentId,
                chunks.Chunks,
                cancellationToken);

            await contentRepository.AddChunksAsync(
                chunks.Chunks
                    .Select(chunk => ContentChunk.Create(
                        contentId,
                        chunk.Index,
                        chunk.Text,
                        chunk.CharStart,
                        chunk.CharEnd,
                        embeddingsByChunkIndex.TryGetValue(chunk.Index, out var embedding)
                            ? embedding
                            : null))
                    .ToList(),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Content {ContentId} was saved but chunk creation/storage failed.",
                contentId);
        }
    }

    private async Task<IReadOnlyDictionary<int, IReadOnlyList<float>>> TryCreateEmbeddingsByChunkIndexAsync(
        Guid contentId,
        IReadOnlyList<TextChunkResult> chunks,
        CancellationToken cancellationToken)
    {
        try
        {
            var embeddings = await aiServiceClient.CreateEmbeddingsAsync(
                new CreateEmbeddingsInput(
                    ContentId: contentId.ToString("N"),
                    Texts: chunks.Select(chunk => chunk.Text).ToList()),
                cancellationToken);

            return embeddings.Embeddings.ToDictionary(
                embedding => embedding.Index,
                embedding => embedding.Embedding);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Content {ContentId} chunks were created but embedding generation failed.",
                contentId);

            return new Dictionary<int, IReadOnlyList<float>>();
        }
    }

    private static void EnsureExtractionIsSaveable(CreateExtractionResult? extraction)
    {
        if (extraction is null ||
            !extraction.ExtractionStatus.Equals("unsupported", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!TryReadExtraValue(extraction, "reason", out var reason) ||
            !reason.Equals("search_result_page", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new UnsupportedContentInputException(
            "Bu Google arama sonucu linki. Bunu kaydetmeyelim; arama sonucunda açtığın gerçek makale, video veya PDF linkini gönder.");
    }

    private static bool TryReadExtraValue(
        CreateExtractionResult extraction,
        string key,
        out string value)
    {
        value = string.Empty;

        if (!extraction.Metadata.Extra.TryGetValue(key, out var rawValue))
        {
            return false;
        }

        value = rawValue?.ToString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }
}
