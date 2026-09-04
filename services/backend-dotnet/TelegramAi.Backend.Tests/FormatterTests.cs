using TelegramAi.Backend.Application.Content.Queries;
using TelegramAi.Backend.Application.Telegram.Formatting;
using TelegramAi.Backend.Domain.Content;
using Xunit;

namespace TelegramAi.Backend.Tests;

public sealed class FormatterTests
{
    [Fact]
    public void SearchFormatter_ReturnsNotFoundMessageForEmptyResults()
    {
        var formatter = new TelegramContentSearchResponseFormatter();
        var query = new SearchContentsQuery([], null, null, null, null);

        var messages = formatter.FormatMessages(query, []);

        Assert.Single(messages);
        Assert.Equal("🔍 Aramana uygun bir kayıt bulamadım.", messages[0]);
    }

    [Fact]
    public void SearchFormatter_KeepsUrlPreviewAndAddsKeywordFilter()
    {
        var formatter = new TelegramContentSearchResponseFormatter();
        var query = new SearchContentsQuery(["rag"], ContentKind.Video, null, null, null);
        var content = ContentItem.Create(
            Guid.NewGuid(),
            ContentSourceType.YouTube,
            ContentKind.Video,
            "https://youtube.com/watch?v=abc",
            ContentSummary.Create(" Video ", " Kısa özet ", [], [], "tr", "test"));

        var messages = formatter.FormatMessages(query, [content]);
        var message = messages[1];

        Assert.Equal(2, messages.Count);
        Assert.Contains("📌 Video", message);
        Assert.Contains("https://youtube.com/watch?v=abc", message);
        Assert.Contains("🏷️ Filtre: rag", message);
    }

    [Fact]
    public void AnswerFormatter_CleansMarkdownAndGroupsChunksBySource()
    {
        var formatter = new TelegramSemanticAnswerResponseFormatter();
        var firstContentId = Guid.NewGuid();
        var secondContentId = Guid.NewGuid();
        var result = new SemanticAnswerResult(
            "soru",
            "**Kalp** etkisi ve [kaynak](https://example.com)",
            "test",
            [0, 1],
            [
                Result(firstContentId, "Kahve", "https://example.com/coffee", 0),
                Result(firstContentId, "Kahve", "https://example.com/coffee", 2),
                Result(secondContentId, "Video", "not-a-url", 1)
            ]);

        var answer = formatter.Format(result);
        var sources = formatter.FormatSourceMessages(result);

        Assert.DoesNotContain("**", answer);
        Assert.Contains("Kalp", answer);
        Assert.Contains("kaynak (https://example.com)", answer);
        Assert.Equal(2, sources.Count);
        Assert.Contains("🧩 Kullanılan chunklar: 0, 2", sources[0]);
        Assert.Contains("https://example.com/coffee", sources[0]);
        Assert.DoesNotContain("not-a-url", sources[1]);
    }

    private static SemanticSearchChunkResult Result(Guid contentId, string title, string url, int chunkIndex)
    {
        return new SemanticSearchChunkResult(
            contentId,
            Guid.NewGuid(),
            title,
            url,
            ContentSourceType.Article,
            ContentKind.Text,
            chunkIndex,
            "metin",
            0.2,
            DateTimeOffset.UtcNow);
    }
}
