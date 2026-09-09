using Microsoft.Extensions.Logging;
using TelegramAi.Backend.Application.Contracts.Chunks;
using TelegramAi.Backend.Application.Contracts.Embeddings;
using TelegramAi.Backend.Application.Contracts.Extractions;
using TelegramAi.Backend.Application.Contracts.Summaries;
using TelegramAi.Backend.Application.Features.Content.Create;
using TelegramAi.Backend.Application.Features.Content.Exceptions;
using TelegramAi.Backend.Application.Features.Content.Policies;
using TelegramAi.Backend.Application.Shared.Abstractions;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Content.Create;

public sealed class ContentCreationWorkflow(
    IAiServiceClient aiServiceClient,
    IContentRepository repository,
    ILogger<ContentCreationWorkflow> logger) : IContentCreationWorkflow
{
    public async Task<ContentItem> ExecuteAsync(CreateContentCommand command, CancellationToken cancellationToken)
    {
        var contentId = Guid.NewGuid();
        var extraction = await TryExtractAsync(contentId, command, cancellationToken);
        EnsureExtractionIsSaveable(command, extraction);
        var summaryText = ContentInputPolicy.ResolveSummaryInputText(command, extraction);
        var chunkText = ContentInputPolicy.ResolveChunkInputText(command, extraction, summaryText);
        var summary = await aiServiceClient.CreateSummaryAsync(new CreateSummaryInput(contentId.ToString("N"), summaryText), cancellationToken);
        var item = ContentItem.Create(contentId, ContentInputPolicy.ResolveSourceType(command, extraction), ContentInputPolicy.ResolveContentKind(command, extraction), command.Text,
            ContentSummary.Create(summary.Title, summary.ShortSummary, summary.KeyPoints, summary.Tags, summary.Language, summary.Provider),
            extraction?.OriginalUrl,
            ResolveImageUrl(extraction),
            extraction?.ReaderBlocks.Select(block => new ReaderBlock(block.Type, block.Text, block.Level, block.Url, block.Caption, block.Items)).ToList());
        await repository.AddAsync(item, cancellationToken);
        await TryCreateChunksAsync(item.Id, chunkText, cancellationToken);
        return item;
    }

    private async Task<CreateExtractionResult?> TryExtractAsync(Guid id, CreateContentCommand command, CancellationToken ct)
    {
        var url = ContentInputPolicy.TryExtractUrl(command.Text);
        if (url is null || command.SourceType is ContentSourceType.Telegram or ContentSourceType.Manual) return null;
        try { return await aiServiceClient.CreateExtractionAsync(new CreateExtractionInput(id.ToString("N"), command.SourceType?.ToString().ToLowerInvariant(), url, command.Text), ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { logger.LogWarning(ex, "Extraction failed for content {ContentId}.", id); return null; }
    }

    private async Task TryCreateChunksAsync(Guid id, string text, CancellationToken ct)
    {
        try
        {
            var chunks = await aiServiceClient.CreateChunksAsync(new CreateChunksInput(id.ToString("N"), text), ct);
            IReadOnlyDictionary<int, IReadOnlyList<float>> embeddings;
            try
            {
                var response = await aiServiceClient.CreateEmbeddingsAsync(new CreateEmbeddingsInput(id.ToString("N"), chunks.Chunks.Select(c => c.Text).ToList()), ct);
                embeddings = response.Embeddings.ToDictionary(e => e.Index, e => e.Embedding);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) { logger.LogWarning(ex, "Embedding generation failed for content {ContentId}.", id); embeddings = new Dictionary<int, IReadOnlyList<float>>(); }
            await repository.AddChunksAsync(chunks.Chunks.Select(c => ContentChunk.Create(id, c.Index, c.Text, c.CharStart, c.CharEnd, embeddings.TryGetValue(c.Index, out var e) ? e : null)).ToList(), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { logger.LogWarning(ex, "Chunk creation failed for content {ContentId}.", id); }
    }

    private static void EnsureExtractionIsSaveable(CreateContentCommand command, CreateExtractionResult? extraction)
    {
        var url = ContentInputPolicy.TryExtractUrl(command.Text);
        if (url is not null && extraction is null)
            throw new UnsupportedContentInputException("İçerik servisine ulaşılamadı; bağlantı kaydedilmedi. Lütfen tekrar dene.");

        if (extraction is null) return;
        if (extraction.ExtractionStatus.Equals("unsupported", StringComparison.OrdinalIgnoreCase)
            && extraction.Metadata.Extra.TryGetValue("reason", out var value)
            && string.Equals(value?.ToString(), "search_result_page", StringComparison.OrdinalIgnoreCase))
            throw new UnsupportedContentInputException("Bu Google arama sonucu linki. Gerçek içerik linkini gönder.");

        if (url is not null
            && !extraction.ExtractionStatus.Equals("completed", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(extraction.ExtractedText))
            throw new UnsupportedContentInputException("İçerik bu bağlantıdan okunamadı; yalnızca URL kaydedilmedi. Lütfen tekrar dene.");
    }

    private static string? ResolveImageUrl(CreateExtractionResult? extraction)
    {
        if (extraction is null) return null;
        foreach (var key in new[] { "thumbnail_url", "image_url" })
        {
            if (extraction.Metadata.Extra.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value?.ToString()))
                return value.ToString();
        }
        return null;
    }
}
