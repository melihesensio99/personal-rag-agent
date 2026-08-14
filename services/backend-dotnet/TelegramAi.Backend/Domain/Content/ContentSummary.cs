namespace TelegramAi.Backend.Domain.Content;

public sealed class ContentSummary
{
    private ContentSummary(
        string title,
        string shortSummary,
        IReadOnlyList<string> keyPoints,
        IReadOnlyList<string> tags,
        string language,
        string provider)
    {
        Title = title;
        ShortSummary = shortSummary;
        KeyPoints = keyPoints;
        Tags = tags;
        Language = language;
        Provider = provider;
    }

    public string Title { get; }
    public string ShortSummary { get; }
    public IReadOnlyList<string> KeyPoints { get; }
    public IReadOnlyList<string> Tags { get; }
    public string Language { get; }
    public string Provider { get; }

    public static ContentSummary Create(
        string title,
        string shortSummary,
        IReadOnlyList<string> keyPoints,
        IReadOnlyList<string> tags,
        string language,
        string provider)
    {
        return new ContentSummary(
            title.Trim(),
            shortSummary.Trim(),
            keyPoints,
            tags,
            language.Trim(),
            provider.Trim());
    }
}
