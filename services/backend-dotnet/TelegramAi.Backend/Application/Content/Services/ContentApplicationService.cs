using TelegramAi.Backend.Api.Contracts.Extractions;
using TelegramAi.Backend.Api.Contracts.Chunks;
using TelegramAi.Backend.Api.Contracts.Answers;
using TelegramAi.Backend.Api.Contracts.Embeddings;
using TelegramAi.Backend.Api.Contracts.Summaries;
using TelegramAi.Backend.Application.Abstractions;
using TelegramAi.Backend.Application.Content.Commands;
using TelegramAi.Backend.Application.Content.Exceptions;
using TelegramAi.Backend.Application.Content.Queries;
using TelegramAi.Backend.Domain.Content;
using TelegramAi.Backend.Infrastructure.AiService;

namespace TelegramAi.Backend.Application.Content.Services;

public sealed class ContentApplicationService(
    IAiServiceClient aiServiceClient,
    IContentRepository contentRepository,
    ILogger<ContentApplicationService> logger) : IContentApplicationService
{
    private const int SemanticCandidateLimit = 20;
    private const int MaxChunksPerContent = 3;
    private const int MaxAnswerChunks = 8;
    private const int MaxAnswerContextCharacters = 12_000;
    private const double MinimumAnswerSimilarity = 0.70;

    public async Task<ContentItem> CreateAsync(
        CreateContentCommand command,
        CancellationToken cancellationToken)
    {
        var contentId = Guid.NewGuid();
        var extraction = await TryExtractAsync(contentId, command, cancellationToken);
        EnsureExtractionIsSaveable(extraction);

        var summaryInputText = ResolveSummaryInputText(command, extraction);
        var chunkInputText = ResolveChunkInputText(command, extraction, summaryInputText);
        var contentKind = ResolveContentKind(command, extraction);
        var sourceType = ResolveSourceType(command, extraction);

        var summary = await aiServiceClient.CreateSummaryAsync(
            new CreateSummaryRequest(
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
        CancellationToken cancellationToken)
    {
        var candidates = await SemanticSearchChunksAsync(
            query,
            Math.Max(maxResults, SemanticCandidateLimit),
            contentId,
            cancellationToken);
        var sources = SelectAnswerSources(candidates, maxResults);

        if (sources.Count == 0)
        {
            return new SemanticAnswerResult(
                Query: query,
                Answer: "Kayıtlı kaynaklarımda bu soruya cevap verecek yeterli bilgi bulunamadı.",
                Provider: "backend",
                UsedChunkIndexes: [],
                Sources: []);
        }

        var answer = await aiServiceClient.CreateAnswerAsync(
            new CreateAnswerRequest(
                ContentId: "semantic-answer-query",
                Question: query,
                Chunks: BuildAnswerChunks(sources)),
            cancellationToken);

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
        var searchDebug = await SemanticSearchChunksDebugAsync(
            query,
            Math.Max(maxResults, SemanticCandidateLimit),
            contentId,
            cancellationToken);

        var selectedSources = SelectAnswerSources(searchDebug.Results, maxResults);
        var contextChunks = BuildAnswerChunks(selectedSources);

        if (contextChunks.Count == 0)
        {
            return new SemanticAnswerDebugResult(
                Query: query,
                EmbeddingModel: searchDebug.EmbeddingModel,
                EmbeddingDimension: searchDebug.EmbeddingDimension,
                QueryEmbeddingPreview: searchDebug.QueryEmbeddingPreview,
                AnswerProvider: "backend",
                Answer: "Kayıtlı kaynaklarımda bu soruya cevap verecek yeterli bilgi bulunamadı.",
                UsedChunkIndexes: [],
                ContextChunksSentToLlm: [],
                Sources: []);
        }

        var answer = await aiServiceClient.CreateAnswerAsync(
            new CreateAnswerRequest(
                ContentId: "semantic-answer-query",
                Question: query,
                Chunks: contextChunks),
            cancellationToken);

        return new SemanticAnswerDebugResult(
            Query: query,
            EmbeddingModel: searchDebug.EmbeddingModel,
            EmbeddingDimension: searchDebug.EmbeddingDimension,
            QueryEmbeddingPreview: searchDebug.QueryEmbeddingPreview,
            AnswerProvider: answer.Provider,
            Answer: answer.Answer,
            UsedChunkIndexes: answer.UsedChunkIndexes,
            ContextChunksSentToLlm: contextChunks,
            Sources: selectedSources);
    }

    private async Task<(CreateEmbeddingsResponse Response, IReadOnlyList<float> QueryEmbedding)> CreateQueryEmbeddingAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var embeddings = await aiServiceClient.CreateEmbeddingsAsync(
            new CreateEmbeddingsRequest(
                ContentId: "semantic-search-query",
                Texts: [query]),
            cancellationToken);

        var queryEmbedding = embeddings.Embeddings.SingleOrDefault()?.Embedding
            ?? throw new InvalidOperationException("AI service did not return a query embedding.");

        return (embeddings, queryEmbedding);
    }

    private static IReadOnlyList<CreateAnswerChunkRequest> BuildAnswerChunks(
        IReadOnlyList<SemanticSearchChunkResult> sources)
    {
        return sources.Select((source, index) => new CreateAnswerChunkRequest(
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

    private static IReadOnlyList<SemanticSearchChunkResult> SelectAnswerSources(
        IReadOnlyList<SemanticSearchChunkResult> candidates,
        int requestedMaxResults)
    {
        var totalLimit = Math.Clamp(requestedMaxResults, 1, MaxAnswerChunks);
        var groups = candidates
            .Where(candidate => Math.Max(0, 1 - candidate.Distance) >= MinimumAnswerSimilarity)
            .GroupBy(candidate => candidate.ContentId)
            .Select(group => group
                .OrderByDescending(candidate => 1 - candidate.Distance)
                .Take(MaxChunksPerContent)
                .ToList())
            .OrderByDescending(group => 1 - group[0].Distance)
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

                var candidate = group[round];
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

    private async Task<CreateExtractionResponse?> TryExtractAsync(
        Guid contentId,
        CreateContentCommand command,
        CancellationToken cancellationToken)
    {
        var url = TryExtractUrl(command.Text);

        if (url is null || command.SourceType is ContentSourceType.Telegram or ContentSourceType.Manual)
        {
            return null;
        }

        try
        {
            return await aiServiceClient.CreateExtractionAsync(
                new CreateExtractionRequest(
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
                new CreateChunksRequest(
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
        IReadOnlyList<TextChunkResponse> chunks,
        CancellationToken cancellationToken)
    {
        try
        {
            var embeddings = await aiServiceClient.CreateEmbeddingsAsync(
                new CreateEmbeddingsRequest(
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

    private static void EnsureExtractionIsSaveable(CreateExtractionResponse? extraction)
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

    private static string ResolveSummaryInputText(
        CreateContentCommand command,
        CreateExtractionResponse? extraction)
    {
        if (!string.IsNullOrWhiteSpace(command.SummaryInputText))
        {
            return command.SummaryInputText.Trim();
        }

        if (extraction is not null &&
            extraction.ExtractionStatus.Equals("completed", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(extraction.ExtractedText))
        {
            return BuildSummaryInputText(extraction);
        }

        return command.Text.Trim();
    }

    private static string ResolveChunkInputText(
        CreateContentCommand command,
        CreateExtractionResponse? extraction,
        string summaryInputText)
    {
        if (extraction is not null &&
            extraction.ExtractionStatus.Equals("completed", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(extraction.ExtractedText))
        {
            return extraction.ExtractedText.Trim();
        }

        if (!string.IsNullOrWhiteSpace(command.SummaryInputText))
        {
            return command.SummaryInputText.Trim();
        }

        return summaryInputText.Trim();
    }

    private static string BuildSummaryInputText(CreateExtractionResponse extraction)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(extraction.Title))
        {
            parts.Add($"Title: {extraction.Title.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(extraction.OriginalUrl))
        {
            parts.Add($"Original URL: {extraction.OriginalUrl.Trim()}");
        }

        parts.Add(extraction.ExtractedText.Trim());

        return string.Join(Environment.NewLine, parts);
    }

    private static ContentKind ResolveContentKind(
        CreateContentCommand command,
        CreateExtractionResponse? extraction)
    {
        if (extraction is not null)
        {
            return ContentKindMapper.FromDetectedContentKind(
                extraction.DetectedContentKind,
                command.SourceType ?? ContentSourceType.Telegram);
        }

        return ContentKindMapper.FromSourceType(command.SourceType ?? ContentSourceType.Telegram);
    }

    private static ContentSourceType ResolveSourceType(
        CreateContentCommand command,
        CreateExtractionResponse? extraction)
    {
        if (extraction is not null &&
            Enum.TryParse<ContentSourceType>(extraction.SourceType, ignoreCase: true, out var extractedSourceType))
        {
            return extractedSourceType;
        }

        return command.SourceType ?? ContentSourceType.Telegram;
    }

    private static string? TryExtractUrl(string text)
    {
        var firstToken = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        return Uri.TryCreate(firstToken, UriKind.Absolute, out var uri)
            ? uri.ToString()
            : null;
    }

    private static bool TryReadExtraValue(
        CreateExtractionResponse extraction,
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
