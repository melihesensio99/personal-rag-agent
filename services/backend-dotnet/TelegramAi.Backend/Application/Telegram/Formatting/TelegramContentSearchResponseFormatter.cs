using System.Text;
using System.Text.RegularExpressions;
using TelegramAi.Backend.Application.Content.Queries;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Telegram.Formatting;

public sealed class TelegramContentSearchResponseFormatter : ITelegramContentSearchResponseFormatter
{
    public string Format(SearchContentsQuery query, IReadOnlyList<ContentItem> contents)
    {
        if (contents.Count == 0)
        {
            return "🔍 Aramana uygun bir kayıt bulamadım.";
        }

        var builder = new StringBuilder();
        builder.AppendLine(contents.Count == 1 ? "🔍 Bunu buldum" : "🔍 Bunları buldum");
        builder.AppendLine();

        foreach (var content in contents)
        {
            builder.AppendLine("────────────────");
            builder.AppendLine($"📌 {content.Summary.Title}");
            builder.AppendLine($"📎 Tür: {content.SourceType}");
            builder.AppendLine($"🕒 Tarih: {content.CreatedAtUtc.ToLocalTime():dd.MM.yyyy HH:mm}");
            builder.AppendLine("📝 Özet");
            builder.AppendLine(content.Summary.ShortSummary);
            builder.AppendLine("🔗 İçerik");
            builder.AppendLine(BuildRawContentPreview(content.RawText));
            builder.AppendLine();
        }

        if (query.Keywords.Count > 0)
        {
            builder.AppendLine($"🏷️ Filtre: {string.Join(", ", query.Keywords)}");
        }

        return builder.ToString().Trim();
    }

    private static string BuildRawContentPreview(string rawText)
    {
        var trimmed = rawText.Trim();

        if (trimmed.Length <= 160)
        {
            return trimmed;
        }

        if (LooksLikeUrl(trimmed))
        {
            return trimmed;
        }

        return $"{trimmed[..157]}...";
    }

    private static bool LooksLikeUrl(string value)
    {
        return Regex.IsMatch(value, @"^https?://", RegexOptions.IgnoreCase);
    }
}
