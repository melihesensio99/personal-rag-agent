namespace TelegramAi.Backend.Domain.Content;

public sealed class ContentItem
{
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

    public Guid Id { get; }
    public ContentSourceType SourceType { get; }
    public string RawText { get; }
    public ContentSummary Summary { get; }
    public DateTimeOffset CreatedAtUtc { get; }

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
