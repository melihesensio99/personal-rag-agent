using System.Text;
using TelegramAi.Backend.Application.Content.Queries;

namespace TelegramAi.Backend.Application.Telegram.Formatting;

public sealed class TelegramSemanticAnswerResponseFormatter : ITelegramSemanticAnswerResponseFormatter
{
    private const int TelegramSafeMessageLength = 3800;

    public string Format(SemanticAnswerResult result)
    {
        var builder = new StringBuilder();

        builder.AppendLine("🤖 Cevap");
        builder.AppendLine(result.Answer);

        if (result.Sources.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("📚 Kaynaklar");

            foreach (var sourceGroup in result.Sources
                         .GroupBy(source => source.ContentId)
                         .Select(group => new
                         {
                             Source = group.First(),
                             ChunkIndexes = group
                                 .Select(source => source.ChunkIndex)
                                 .Distinct()
                                 .OrderBy(index => index)
                                 .ToArray(),
                         }))
            {
                builder.AppendLine("────────────────");
                builder.AppendLine($"📌 {sourceGroup.Source.ContentTitle}");
                builder.AppendLine($"📎 Tür: {sourceGroup.Source.SourceType}");

                if (IsHttpUrl(sourceGroup.Source.ContentUrl))
                {
                    builder.AppendLine($"🔗 {sourceGroup.Source.ContentUrl}");
                }

                builder.AppendLine($"🧩 Kullanılan chunklar: {string.Join(", ", sourceGroup.ChunkIndexes)}");
            }
        }

        return TruncateForTelegram(builder.ToString().Trim());
    }

    private static string TruncateForTelegram(string message)
    {
        if (message.Length <= TelegramSafeMessageLength)
        {
            return message;
        }

        return $"{message[..TelegramSafeMessageLength]}\n\n…";
    }

    private static bool IsHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
