namespace TelegramAi.Backend.Domain.Content;

public sealed class ContentItem
{
    private ContentItem()
    {
        RawText = string.Empty;
        ContentKind = ContentKind.Text;
        Summary = null!;
        ReaderBlocks = [];
    }

    private ContentItem(
        Guid id,
        ContentSourceType sourceType,
        ContentKind contentKind,
        string rawText,
        ContentSummary summary,
        DateTimeOffset createdAtUtc,
        string? originalUrl,
        string? imageUrl,
        IReadOnlyList<ReaderBlock> readerBlocks)
    {
        Id = id;
        SourceType = sourceType;
        ContentKind = contentKind;
        RawText = rawText;
        Summary = summary;
        CreatedAtUtc = createdAtUtc;
        OriginalUrl = originalUrl;
        ImageUrl = imageUrl;
        ReaderBlocks = readerBlocks;
    }

    public Guid Id { get; private set; }
    public ContentSourceType SourceType { get; private set; }
    public ContentKind ContentKind { get; private set; }
    public string RawText { get; private set; }
    public ContentSummary Summary { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public string? OriginalUrl { get; private set; }
    public string? ImageUrl { get; private set; }
    public IReadOnlyList<ReaderBlock> ReaderBlocks { get; private set; }

    public static ContentItem Create(
        Guid id,
        ContentSourceType sourceType,
        ContentKind contentKind,
        string rawText,
        ContentSummary summary,
        string? originalUrl = null,
        string? imageUrl = null,
        IReadOnlyList<ReaderBlock>? readerBlocks = null)
    {
        return new ContentItem(
            id,
            sourceType,
            contentKind,
            rawText.Trim(),
            summary,
            DateTimeOffset.UtcNow,
            string.IsNullOrWhiteSpace(originalUrl) ? null : originalUrl.Trim(),
            string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim(),
            readerBlocks?.ToList() ?? []);
    }
}
