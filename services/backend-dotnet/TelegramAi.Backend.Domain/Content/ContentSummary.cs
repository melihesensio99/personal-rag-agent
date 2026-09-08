namespace TelegramAi.Backend.Domain.Content;

public sealed class ContentSummary
{
    private const int MaxTitleLength = 200;
    private const int MaxLanguageLength = 10;
    private const int MaxProviderLength = 50;
    private const int MaxKeyPointLength = 500;
    private const int MaxTagLength = 80;
    private const int MaxKeyPointCount = 8;
    private const int MaxTagCount = 10;

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
            Truncate(title, MaxTitleLength),
            shortSummary.Trim(),
            keyPoints
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => Truncate(value, MaxKeyPointLength))
                .Take(MaxKeyPointCount)
                .ToList(),
            tags
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => Truncate(value, MaxTagLength))
                .Take(MaxTagCount)
                .ToList(),
            Truncate(language, MaxLanguageLength),
            Truncate(provider, MaxProviderLength));
    }

    private static string Truncate(string value, int maxLength)
    {
        var trimmed = value.Trim();

        return trimmed.Length <= maxLength
            ? trimmed
            : trimmed[..maxLength];
    }
}
