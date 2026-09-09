using System.Text;
using System.Text.RegularExpressions;
using System.Net;
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
        var b = new StringBuilder("✅ <b>Kaydettim</b>\n\n");
        b.AppendLine($"📎 <b>Tür:</b> {Encode(result.Content.SourceType.ToString())}\n");
        b.AppendLine("🧠 <b>ANA BAŞLIK</b>"); b.AppendLine(Encode(CleanGeneratedText(result.Content.Summary.Title)));
        AppendSummarySections(b, result.Content.Summary.ShortSummary);
        if (result.Content.Summary.KeyPoints.Count > 0)
        {
            b.AppendLine("\n🔎 <b>ANA BULGULAR</b>");
            foreach (var point in result.Content.Summary.KeyPoints.Take(8)) b.AppendLine($"• {Encode(CleanGeneratedText(point))}");
        }
        if (result.Content.Summary.Tags.Count > 0)
            b.AppendLine($"\n🏷️ <b>KONULAR:</b> {Encode(string.Join(" • ", result.Content.Summary.Tags.Take(6).Select(CleanGeneratedText)))}");
        return Truncate(b.ToString().Trim());
    }

    private static void AppendSummarySections(StringBuilder builder, string summary)
    {
        var lines = Regex.Split(CleanGeneratedText(summary), @"\r?\n+")
            .Select(line => Regex.Replace(line.Trim(), @"^[-•]\s*", string.Empty))
            .Where(line => line.Length > 0)
            .ToList();
        if (lines.Count == 0) return;

        builder.AppendLine("\n📋 <b>ANA KONU VE DEĞERLENDİRME</b>");
        foreach (var line in lines)
        {
            var separator = line.IndexOf(':');
            if (separator > 0 && separator < 70)
            {
                builder.AppendLine($"\n<b>{Encode(line[..separator].Trim().ToUpperInvariant())}</b>");
                builder.AppendLine(Encode(line[(separator + 1)..].Trim()));
            }
            else builder.AppendLine(Encode(line));
        }
    }

    public IReadOnlyList<string> FormatSearch(FindContentsQuery query, IReadOnlyList<ContentItem> contents)
    {
        if (contents.Count == 0) return ["🔍 Aramana uygun bir kayıt bulamadım."];
        var messages = new List<string>(contents.Count + 1) { contents.Count == 1 ? "🔍 <b>Bunu buldum</b>" : $"🔍 <b>{contents.Count} kayıt buldum</b>" };
        foreach (var content in contents)
        {
            var b = new StringBuilder();
            b.AppendLine("────────────────"); b.AppendLine($"📌 <b>{Encode(content.Summary.Title)}</b>");
            b.AppendLine($"📎 <b>Tür:</b> {Encode(content.SourceType.ToString())}"); b.AppendLine($"🗂️ <b>İçerik tipi:</b> {Encode(content.ContentKind.ToString())}");
            b.AppendLine($"🕒 <b>Tarih:</b> {content.CreatedAtUtc.ToLocalTime():dd.MM.yyyy HH:mm}");
            b.AppendLine("📝 <b>Özet</b>"); b.AppendLine(Encode(content.Summary.ShortSummary));
            b.AppendLine("🔗 <b>İçerik</b>"); b.AppendLine(Encode(RawPreview(content.RawText)));
            if (query.Keywords.Count > 0) b.AppendLine($"🏷️ <b>Filtre:</b> {Encode(string.Join(", ", query.Keywords))}");
            messages.Add(Truncate(b.ToString().Trim()));
        }
        return messages;
    }

    public string FormatAnswer(SemanticAnswerResult result)
    {
        var b = new StringBuilder("🤖 <b>Cevap</b>\n"); b.AppendLine(Encode(CleanAnswer(result.Answer)));
        if (result.Sources.Count > 0) b.AppendLine("\n📚 <i>Kaynaklar aşağıdaki mesajlarda gösteriliyor.</i>");
        return Truncate(b.ToString().Trim());
    }

    public IReadOnlyList<string> FormatAnswerSources(SemanticAnswerResult result)
    {
        if (result.UsedChunkIndexes.Count == 0) return [];

        var usedIndexes = result.UsedChunkIndexes.ToHashSet();
        var sources = result.Sources
            .Select((source, contextIndex) => (source, contextIndex))
            .Where(item => usedIndexes.Contains(item.contextIndex))
            .Select(item => item.source)
            .ToList();

        return sources.GroupBy(x => x.ContentId).Select(group =>
    {
        var source = group.First(); var indexes = group.Select(x => x.ChunkIndex).Distinct().OrderBy(x => x);
        var b = new StringBuilder("────────────────\n"); b.AppendLine($"📌 <b>{Encode(source.ContentTitle)}</b>"); b.AppendLine($"📎 <b>Tür:</b> {Encode(source.SourceType.ToString())}");
        if (IsHttpUrl(source.ContentUrl)) b.AppendLine($"🔗 <a href=\"{Encode(source.ContentUrl)}\">Kaynağı aç</a>");
        b.AppendLine($"🧩 <b>Kullanılan chunklar:</b> {string.Join(", ", indexes)}"); return Truncate(b.ToString().Trim());
        }).ToList();
    }

    private static string RawPreview(string text) { var value = text.Trim(); return value.Length <= 160 || LooksLikeUrl(value) ? value : $"{value[..157]}..."; }
    private static bool LooksLikeUrl(string value) => Regex.IsMatch(value, @"^https?://", RegexOptions.IgnoreCase);
    private static bool IsHttpUrl(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    private static string Encode(string value) => WebUtility.HtmlEncode(value);
    private static string CleanAnswer(string answer)
    {
        var value = NormalizeMarkdown(answer);
        value = Regex.Replace(value, @"\[([^\]]+)\]\((https?://[^)]+)\)", "$1 ($2)");
        return value;
    }

    private static string CleanGeneratedText(string value) => NormalizeMarkdown(value);

    private static string NormalizeMarkdown(string value)
    {
        // Some providers return Markdown that was escaped before it reached the JSON payload
        // (for example, \"\\**başlık\\**\"). Normalize those markers before removing styling.
        var normalized = Regex.Replace(value.Trim(), @"\\([\\`*_{}\[\]()#+\-.!>])", "$1");
        normalized = Regex.Replace(normalized, @"\*\*|__|`", string.Empty);
        normalized = Regex.Replace(normalized, @"(?m)^\s*#{1,6}\s*", string.Empty);
        return Regex.Replace(normalized, @"(?m)^\s*[•*]\s*", "• ");
    }
    private static string Truncate(string value) => value.Length <= MaxLength ? value : $"{value[..MaxLength]}\n\n…";
}
