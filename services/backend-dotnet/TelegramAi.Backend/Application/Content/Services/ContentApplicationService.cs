using TelegramAi.Backend.Api.Contracts.Extractions;
using TelegramAi.Backend.Api.Contracts.Chunks;
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
        CancellationToken cancellationToken)
    {
        var embeddings = await aiServiceClient.CreateEmbeddingsAsync(
            new CreateEmbeddingsRequest(
                ContentId: "semantic-search-query",
                Texts: [query]),
            cancellationToken);

        var queryEmbedding = embeddings.Embeddings.SingleOrDefault()?.Embedding
            ?? throw new InvalidOperationException("AI service did not return a query embedding.");

        return await contentRepository.SemanticSearchChunksAsync(
            new SemanticSearchChunksQuery(
                Embedding: queryEmbedding,
                MaxResults: maxResults),
            cancellationToken);
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
