using System.Text.RegularExpressions;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Telegram.Classification;

public sealed partial class TelegramContentSourceDetector : ITelegramContentSourceDetector
{
    public ContentSourceType Detect(string messageText)
    {
        var trimmed = messageText.Trim();

        if (!TryExtractUrl(trimmed, out var uri))
        {
            return ContentSourceType.Telegram;
        }

        if (IsYouTube(uri))
        {
            return ContentSourceType.YouTube;
        }

        if (IsPdf(uri))
        {
            return ContentSourceType.Pdf;
        }

        if (IsImage(uri))
        {
            return ContentSourceType.Image;
        }

        return ContentSourceType.Article;
    }

    private static bool TryExtractUrl(string text, out Uri uri)
    {
        var match = UrlRegex().Match(text);
        if (!match.Success)
        {
            uri = null!;
            return false;
        }

        if (Uri.TryCreate(match.Value, UriKind.Absolute, out var parsedUri) && parsedUri is not null)
        {
            uri = parsedUri;
            return true;
        }

        uri = null!;
        return false;
    }

    private static bool IsYouTube(Uri uri)
    {
        var host = uri.Host.ToLowerInvariant();
        return host.Contains("youtube.com", StringComparison.Ordinal) ||
               host.Contains("youtu.be", StringComparison.Ordinal);
    }

    private static bool IsPdf(Uri uri)
    {
        return uri.AbsolutePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsImage(Uri uri)
    {
        return uri.AbsolutePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
               uri.AbsolutePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
               uri.AbsolutePath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
               uri.AbsolutePath.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
               uri.AbsolutePath.EndsWith(".gif", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"https?://\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex UrlRegex();
}
