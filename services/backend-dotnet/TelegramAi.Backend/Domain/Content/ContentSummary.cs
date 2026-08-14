namespace TelegramAi.Backend.Domain.Content;

public sealed class ContentSummary
{
    private ContentSummary()
    {
        Title = string.Empty;
        ShortSummary = string.Empty;
        KeyPoints = [];
        Tags = [];
        Language = string.Empty;
        Provider = string.Empty;
    }

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

    public string Title { get; private set; }
    public string ShortSummary { get; private set; }
    public IReadOnlyList<string> KeyPoints { get; private set; }
    public IReadOnlyList<string> Tags { get; private set; }
    public string Language { get; private set; }
    public string Provider { get; private set; }

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
