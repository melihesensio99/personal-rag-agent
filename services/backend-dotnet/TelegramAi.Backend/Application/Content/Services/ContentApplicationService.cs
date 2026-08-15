using TelegramAi.Backend.Api.Contracts.Extractions;
using TelegramAi.Backend.Api.Contracts.Summaries;
using TelegramAi.Backend.Application.Abstractions;
using TelegramAi.Backend.Application.Content.Commands;
using TelegramAi.Backend.Application.Content.Queries;
using TelegramAi.Backend.Domain.Content;
using TelegramAi.Backend.Infrastructure.AiService;

namespace TelegramAi.Backend.Application.Content.Services;

public sealed class ContentApplicationService(
    IAiServiceClient aiServiceClient,
    IContentRepository contentRepository) : IContentApplicationService
{
    public async Task<ContentItem> CreateAsync(
        CreateContentCommand command,
        CancellationToken cancellationToken)
    {
        var contentId = Guid.NewGuid();
        var extraction = await TryExtractAsync(contentId, command, cancellationToken);
        var summaryInputText = ResolveSummaryInputText(command, extraction);

        var summary = await aiServiceClient.CreateSummaryAsync(
            new CreateSummaryRequest(
                ContentId: contentId.ToString("N"),
                Text: summaryInputText),
            cancellationToken);

        var contentItem = ContentItem.Create(
            id: contentId,
            sourceType: command.SourceType,
            rawText: command.Text,
            summary: ContentSummary.Create(
                title: summary.Title,
                shortSummary: summary.ShortSummary,
                keyPoints: summary.KeyPoints,
                tags: summary.Tags,
                language: summary.Language,
                provider: summary.Provider));

        await contentRepository.AddAsync(contentItem, cancellationToken);

        return contentItem;
    }

    public Task<ContentItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return contentRepository.GetByIdAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<ContentItem>> SearchAsync(
        SearchContentsQuery query,
        CancellationToken cancellationToken)
    {
        return contentRepository.SearchAsync(query, cancellationToken);
    }

    private async Task<CreateExtractionResponse?> TryExtractAsync(
        Guid contentId,
        CreateContentCommand command,
        CancellationToken cancellationToken)
    {
        if (command.SourceType is ContentSourceType.Telegram or ContentSourceType.Manual)
        {
            return null;
        }

        return await aiServiceClient.CreateExtractionAsync(
            new CreateExtractionRequest(
                ContentId: contentId.ToString("N"),
                SourceType: command.SourceType.ToString().ToLowerInvariant(),
                Url: TryExtractUrl(command.Text),
                Text: command.Text),
            cancellationToken);
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
            return extraction.ExtractedText.Trim();
        }

        return command.Text.Trim();
    }

    private static string? TryExtractUrl(string text)
    {
        var firstToken = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        return Uri.TryCreate(firstToken, UriKind.Absolute, out var uri)
            ? uri.ToString()
            : null;
    }
}
