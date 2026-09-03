using System.Text;
using System.Text.RegularExpressions;
using TelegramAi.Backend.Application.Content.Queries;

namespace TelegramAi.Backend.Application.Telegram.Formatting;

public sealed class TelegramSemanticAnswerResponseFormatter : ITelegramSemanticAnswerResponseFormatter
{
    private const int TelegramSafeMessageLength = 3800;

    public string Format(SemanticAnswerResult result)
    {
        var builder = new StringBuilder();

        builder.AppendLine("🤖 Cevap");
        builder.AppendLine(CleanLlmAnswer(result.Answer));

        if (result.Sources.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("📚 Kaynaklar aşağıdaki mesajlarda gösteriliyor.");
        }

        return TruncateForTelegram(builder.ToString().Trim());
    }

    public IReadOnlyList<string> FormatSourceMessages(SemanticAnswerResult result)
    {
        return result.Sources
            .GroupBy(source => source.ContentId)
            .Select(group =>
            {
                var source = group.First();
                var chunkIndexes = group
                    .Select(item => item.ChunkIndex)
                    .Distinct()
                    .OrderBy(index => index)
                    .ToArray();
                var builder = new StringBuilder();

                builder.AppendLine("────────────────");
                builder.AppendLine($"📌 {source.ContentTitle}");
                builder.AppendLine($"📎 Tür: {source.SourceType}");

                if (IsHttpUrl(source.ContentUrl))
                {
                    builder.AppendLine($"🔗 {source.ContentUrl}");
                }

                builder.AppendLine($"🧩 Kullanılan chunklar: {string.Join(", ", chunkIndexes)}");
                return TruncateForTelegram(builder.ToString().Trim());
            })
            .ToList();
    }

    private static string TruncateForTelegram(string message)
    {
        if (message.Length <= TelegramSafeMessageLength)
        {
            return message;
        }

        return $"{message[..TelegramSafeMessageLength]}\n\n…";
    }

    private static string CleanLlmAnswer(string answer)
    {
        var cleaned = answer.Trim();

        // Telegram plain-text output should not expose Markdown control characters.
        cleaned = Regex.Replace(cleaned, @"\*\*|__|`", string.Empty);
        cleaned = Regex.Replace(cleaned, @"(?m)^\s*#{1,6}\s*", string.Empty);
        cleaned = Regex.Replace(cleaned, @"\[([^\]]+)\]\((https?://[^)]+)\)", "$1 ($2)");

        return cleaned;
    }

    private static bool IsHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
