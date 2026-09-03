using System.Text;
using System.Text.RegularExpressions;
using TelegramAi.Backend.Application.Content.Queries;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Telegram.Formatting;

public sealed class TelegramContentSearchResponseFormatter : ITelegramContentSearchResponseFormatter
{
    private const int TelegramSafeMessageLength = 3800;

    public IReadOnlyList<string> FormatMessages(SearchContentsQuery query, IReadOnlyList<ContentItem> contents)
    {
        if (contents.Count == 0)
        {
            return ["🔍 Aramana uygun bir kayıt bulamadım."];
        }

        var messages = new List<string>(contents.Count + 1)
        {
            contents.Count == 1 ? "🔍 Bunu buldum" : $"🔍 {contents.Count} kayıt buldum"
        };

        foreach (var content in contents)
        {
            var builder = new StringBuilder();
            builder.AppendLine("────────────────");
            builder.AppendLine($"📌 {content.Summary.Title}");
            builder.AppendLine($"📎 Tür: {content.SourceType}");
            builder.AppendLine($"🗂️ İçerik tipi: {content.ContentKind}");
            builder.AppendLine($"🕒 Tarih: {content.CreatedAtUtc.ToLocalTime():dd.MM.yyyy HH:mm}");
            builder.AppendLine("📝 Özet");
            builder.AppendLine(content.Summary.ShortSummary);
            builder.AppendLine("🔗 İçerik");
            builder.AppendLine(BuildRawContentPreview(content.RawText));
            if (query.Keywords.Count > 0)
            {
                builder.AppendLine($"🏷️ Filtre: {string.Join(", ", query.Keywords)}");
            }

            messages.Add(TruncateForTelegram(builder.ToString().Trim()));
        }

        return messages;
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

    private static string TruncateForTelegram(string message)
    {
        if (message.Length <= TelegramSafeMessageLength)
        {
            return message;
        }

        return $"{message[..TelegramSafeMessageLength]}\n\n…";
    }
}
