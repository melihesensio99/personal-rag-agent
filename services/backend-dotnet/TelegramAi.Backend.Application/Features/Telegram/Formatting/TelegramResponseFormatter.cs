using System.Text;
using System.Text.RegularExpressions;
using TelegramAi.Backend.Application.Features.Content.Discovery;
using TelegramAi.Backend.Application.Features.Content.SemanticAnswer;
using TelegramAi.Backend.Application.Features.Telegram.Process;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Telegram.Formatting;

public sealed class TelegramResponseFormatter : ITelegramResponseFormatter
{
    private const int MaxLength = 3800;

    public string Format(ProcessTelegramMessageResult result)
    {
        var b = new StringBuilder("✅ Kaydettim\n\n");
        b.AppendLine($"📎 Tür: {result.Content.SourceType}\n");
        b.AppendLine("🧠 Başlık"); b.AppendLine(result.Content.Summary.Title);
        b.AppendLine("\n📝 Özet"); b.AppendLine(result.Content.Summary.ShortSummary);
        if (result.Content.Summary.KeyPoints.Count > 0)
        {
            b.AppendLine("\n🔹 Ana noktalar");
            foreach (var point in result.Content.Summary.KeyPoints) b.AppendLine($"• {point}");
        }
        return Truncate(b.ToString().Trim());
    }

    public IReadOnlyList<string> FormatSearch(FindContentsQuery query, IReadOnlyList<ContentItem> contents)
    {
        if (contents.Count == 0) return ["🔍 Aramana uygun bir kayıt bulamadım."];
        var messages = new List<string>(contents.Count + 1) { contents.Count == 1 ? "🔍 Bunu buldum" : $"🔍 {contents.Count} kayıt buldum" };
        foreach (var content in contents)
        {
            var b = new StringBuilder();
            b.AppendLine("────────────────"); b.AppendLine($"📌 {content.Summary.Title}");
            b.AppendLine($"📎 Tür: {content.SourceType}"); b.AppendLine($"🗂️ İçerik tipi: {content.ContentKind}");
            b.AppendLine($"🕒 Tarih: {content.CreatedAtUtc.ToLocalTime():dd.MM.yyyy HH:mm}");
            b.AppendLine("📝 Özet"); b.AppendLine(content.Summary.ShortSummary);
            b.AppendLine("🔗 İçerik"); b.AppendLine(RawPreview(content.RawText));
            if (query.Keywords.Count > 0) b.AppendLine($"🏷️ Filtre: {string.Join(", ", query.Keywords)}");
            messages.Add(Truncate(b.ToString().Trim()));
        }
        return messages;
    }

    public string FormatAnswer(SemanticAnswerResult result)
    {
        var b = new StringBuilder("🤖 Cevap\n"); b.AppendLine(CleanAnswer(result.Answer));
        if (result.Sources.Count > 0) b.AppendLine("\n📚 Kaynaklar aşağıdaki mesajlarda gösteriliyor.");
        return Truncate(b.ToString().Trim());
    }

    public IReadOnlyList<string> FormatAnswerSources(SemanticAnswerResult result) => result.Sources.GroupBy(x => x.ContentId).Select(group =>
    {
        var source = group.First(); var indexes = group.Select(x => x.ChunkIndex).Distinct().OrderBy(x => x);
        var b = new StringBuilder("────────────────\n"); b.AppendLine($"📌 {source.ContentTitle}"); b.AppendLine($"📎 Tür: {source.SourceType}");
        if (IsHttpUrl(source.ContentUrl)) b.AppendLine($"🔗 {source.ContentUrl}");
        b.AppendLine($"🧩 Kullanılan chunklar: {string.Join(", ", indexes)}"); return Truncate(b.ToString().Trim());
    }).ToList();

    private static string RawPreview(string text) { var value = text.Trim(); return value.Length <= 160 || LooksLikeUrl(value) ? value : $"{value[..157]}..."; }
    private static bool LooksLikeUrl(string value) => Regex.IsMatch(value, @"^https?://", RegexOptions.IgnoreCase);
    private static bool IsHttpUrl(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    private static string CleanAnswer(string answer) { var value = Regex.Replace(answer.Trim(), @"\*\*|__|`", string.Empty); value = Regex.Replace(value, @"(?m)^\s*#{1,6}\s*", string.Empty); return Regex.Replace(value, @"\[([^\]]+)\]\((https?://[^)]+)\)", "$1 ($2)"); }
    private static string Truncate(string value) => value.Length <= MaxLength ? value : $"{value[..MaxLength]}\n\n…";
}
