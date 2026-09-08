using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Api.Validation;

internal static class ContentSourceTypeParser
{
    public static bool TryParse(string? value, out ContentSourceType? sourceType)
    {
        sourceType = null;
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (!Enum.TryParse<ContentSourceType>(value, true, out var parsed)) return false;
        sourceType = parsed;
        return true;
    }
}
