namespace TelegramAi.Backend.Domain.Content;

public sealed class ContentItem
{
    private ContentItem()
    {
        RawText = string.Empty;
        Summary = null!;
    }

    private ContentItem(
        Guid id,
        ContentSourceType sourceType,
        string rawText,
        ContentSummary summary,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        SourceType = sourceType;
        RawText = rawText;
        Summary = summary;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public ContentSourceType SourceType { get; private set; }
    public string RawText { get; private set; }
    public ContentSummary Summary { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static ContentItem Create(
        Guid id,
        ContentSourceType sourceType,
        string rawText,
        ContentSummary summary)
    {
        return new ContentItem(
            id,
            sourceType,
            rawText.Trim(),
            summary,
            DateTimeOffset.UtcNow);
    }
}
