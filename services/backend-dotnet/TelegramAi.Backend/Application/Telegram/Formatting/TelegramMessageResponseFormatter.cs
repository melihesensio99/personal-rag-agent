using System.Text;
using TelegramAi.Backend.Application.Telegram.Results;

namespace TelegramAi.Backend.Application.Telegram.Formatting;

public sealed class TelegramMessageResponseFormatter : ITelegramMessageResponseFormatter
{
    private const int TelegramSafeMessageLength = 3800;

    public string Format(ProcessTelegramMessageResult result)
    {
        var builder = new StringBuilder();

        builder.AppendLine("✅ Kaydettim");
        builder.AppendLine();
        builder.AppendLine($"📎 Tür: {result.Content.SourceType}");
        builder.AppendLine();
        builder.AppendLine("🧠 Başlık");
        builder.AppendLine(result.Content.Summary.Title);
        builder.AppendLine();
        builder.AppendLine("📝 Özet");
        builder.AppendLine(result.Content.Summary.ShortSummary);

        if (result.Content.Summary.KeyPoints.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("🔹 Ana noktalar");

            foreach (var keyPoint in result.Content.Summary.KeyPoints)
            {
                builder.AppendLine($"• {keyPoint}");
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
}
