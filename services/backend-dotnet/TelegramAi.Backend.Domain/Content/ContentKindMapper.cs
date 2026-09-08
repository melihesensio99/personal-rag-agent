namespace TelegramAi.Backend.Domain.Content;

public static class ContentKindMapper
{
    public static ContentKind FromSourceType(ContentSourceType sourceType)
    {
        return sourceType switch
        {
            ContentSourceType.YouTube => ContentKind.Video,
            ContentSourceType.Instagram => ContentKind.Video,
            ContentSourceType.Image => ContentKind.Image,
            _ => ContentKind.Text,
        };
    }

    public static ContentKind FromDetectedContentKind(string? detectedContentKind, ContentSourceType fallbackSourceType)
    {
        if (string.Equals(detectedContentKind, "video", StringComparison.OrdinalIgnoreCase))
        {
            return ContentKind.Video;
        }

        if (string.Equals(detectedContentKind, "image", StringComparison.OrdinalIgnoreCase))
        {
            return ContentKind.Image;
        }

        if (string.Equals(detectedContentKind, "text", StringComparison.OrdinalIgnoreCase))
        {
            return ContentKind.Text;
        }

        return FromSourceType(fallbackSourceType);
    }
}
