namespace TelegramAi.Backend.Domain.Content;

public sealed class ContentItem
{
    private ContentItem()
    {
        RawText = string.Empty;
        ContentKind = ContentKind.Text;
        Summary = null!;
    }

    private ContentItem(
        Guid id,
        ContentSourceType sourceType,
        ContentKind contentKind,
        string rawText,
        ContentSummary summary,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        SourceType = sourceType;
        ContentKind = contentKind;
        RawText = rawText;
        Summary = summary;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public ContentSourceType SourceType { get; private set; }
    public ContentKind ContentKind { get; private set; }
    public string RawText { get; private set; }
    public ContentSummary Summary { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static ContentItem Create(
        Guid id,
        ContentSourceType sourceType,
        ContentKind contentKind,
        string rawText,
        ContentSummary summary)
    {
        return new ContentItem(
            id,
            sourceType,
            contentKind,
            rawText.Trim(),
            summary,
            DateTimeOffset.UtcNow);
    }
}
