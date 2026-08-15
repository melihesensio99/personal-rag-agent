using System.Text.RegularExpressions;
using TelegramAi.Backend.Application.Content.Queries;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Telegram.Interpretation;

public sealed class RuleBasedTelegramMessageIntentInterpreter : ITelegramMessageIntentInterpreter
{
    private static readonly string[] SearchMarkers =
    [
        "neydi",
        "hangi",
        "goster",
        "göster",
        "listele",
        "bul",
        "linki",
        "linkleri",
        "linklerim",
        "url",
        "urlsi",
        "url'si"
    ];

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "dun",
        "dün",
        "bugun",
        "bugün",
        "attigim",
        "attığım",
        "attiklarim",
        "attıklarım",
        "olan",
        "olanlar",
        "ile",
        "ilgili",
        "videolari",
        "videoları",
        "video",
        "videoyu",
        "linki",
        "linkleri",
        "linklerim",
        "url",
        "neydi",
        "hangi",
        "goster",
        "göster",
        "listele",
        "bul",
        "bana",
        "gosterir",
        "misin",
        "mısın",
        "vardi",
        "vardı",
        "var",
        "benim",
        "attigim",
        "attığım",
        "youtube",
        "pdf",
        "telegram",
        "mesaj",
        "mesaji",
        "mesajı",
        "makale",
        "gorsel",
        "görsel",
        "resim"
    };

    public TelegramMessageIntent Interpret(string messageText)
    {
        var normalizedText = messageText.Trim();
        var lowered = normalizedText.ToLowerInvariant();

        if (!IsSearchIntent(lowered))
        {
            return new SaveContentIntent(normalizedText);
        }

        var sourceType = ExtractSourceType(lowered);
        var (fromUtc, toUtc) = ExtractDateRange(lowered);
        var keywords = ExtractKeywords(normalizedText);

        return new SearchContentsIntent(
            normalizedText,
            new SearchContentsQuery(
                Keywords: keywords,
                SourceType: sourceType,
                FromUtc: fromUtc,
                ToUtc: toUtc));
    }

    private static bool IsSearchIntent(string lowered)
    {
        if (lowered.StartsWith("/find ", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return SearchMarkers.Any(marker => lowered.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static ContentSourceType? ExtractSourceType(string lowered)
    {
        if (lowered.Contains("youtube", StringComparison.OrdinalIgnoreCase))
        {
            return ContentSourceType.YouTube;
        }

        if (lowered.Contains("pdf", StringComparison.OrdinalIgnoreCase))
        {
            return ContentSourceType.Pdf;
        }

        if (lowered.Contains("makale", StringComparison.OrdinalIgnoreCase) ||
            lowered.Contains("article", StringComparison.OrdinalIgnoreCase))
        {
            return ContentSourceType.Article;
        }

        if (lowered.Contains("gorsel", StringComparison.OrdinalIgnoreCase) ||
            lowered.Contains("görsel", StringComparison.OrdinalIgnoreCase) ||
            lowered.Contains("resim", StringComparison.OrdinalIgnoreCase) ||
            lowered.Contains("image", StringComparison.OrdinalIgnoreCase))
        {
            return ContentSourceType.Image;
        }

        if (lowered.Contains("telegram", StringComparison.OrdinalIgnoreCase) ||
            lowered.Contains("mesaj", StringComparison.OrdinalIgnoreCase))
        {
            return ContentSourceType.Telegram;
        }

        return null;
    }

    private static (DateTimeOffset? FromUtc, DateTimeOffset? ToUtc) ExtractDateRange(string lowered)
    {
        var timeZone = ResolveTurkeyTimeZone();
        var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone);

        if (lowered.Contains("dün", StringComparison.OrdinalIgnoreCase) ||
            lowered.Contains("dun", StringComparison.OrdinalIgnoreCase))
        {
            return BuildDayRange(now.AddDays(-1), timeZone);
        }

        if (lowered.Contains("bugün", StringComparison.OrdinalIgnoreCase) ||
            lowered.Contains("bugun", StringComparison.OrdinalIgnoreCase))
        {
            return BuildDayRange(now, timeZone);
        }

        if (lowered.Contains("geçen hafta", StringComparison.OrdinalIgnoreCase) ||
            lowered.Contains("gecen hafta", StringComparison.OrdinalIgnoreCase))
        {
            var startLocal = now.Date.AddDays(-7);
            var endLocal = now.Date.AddDays(1);
            return (
                TimeZoneInfo.ConvertTimeToUtc(startLocal, timeZone),
                TimeZoneInfo.ConvertTimeToUtc(endLocal, timeZone));
        }

        return (null, null);
    }

    private static IReadOnlyList<string> ExtractKeywords(string text)
    {
        var words = Regex.Matches(text.ToLowerInvariant(), @"\p{L}[\p{L}\p{Nd}]+")
            .Select(match => match.Value)
            .Where(word => word.Length >= 3 && !StopWords.Contains(word))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();

        return words;
    }

    private static (DateTimeOffset FromUtc, DateTimeOffset ToUtc) BuildDayRange(
        DateTimeOffset localDateTime,
        TimeZoneInfo timeZone)
    {
        var dayStart = localDateTime.Date;
        var dayEnd = dayStart.AddDays(1);

        return (
            TimeZoneInfo.ConvertTimeToUtc(dayStart, timeZone),
            TimeZoneInfo.ConvertTimeToUtc(dayEnd, timeZone));
    }

    private static TimeZoneInfo ResolveTurkeyTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        }
    }
}
