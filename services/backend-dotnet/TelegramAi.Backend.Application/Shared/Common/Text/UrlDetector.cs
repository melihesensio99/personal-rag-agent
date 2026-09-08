namespace TelegramAi.Backend.Application.Shared.Common.Text;

public static class UrlDetector
{
    public static bool ContainsUrl(string? text) =>
        !string.IsNullOrWhiteSpace(text) &&
        (text.Contains("http://", StringComparison.OrdinalIgnoreCase) ||
         text.Contains("https://", StringComparison.OrdinalIgnoreCase));

    public static string? TryExtract(string? text)
    {
        var firstToken = text?.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        return Uri.TryCreate(firstToken, UriKind.Absolute, out var uri) ? uri.ToString() : null;
    }
}
