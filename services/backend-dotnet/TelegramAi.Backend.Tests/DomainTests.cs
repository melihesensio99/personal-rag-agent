using TelegramAi.Backend.Domain.Content;
using Xunit;

namespace TelegramAi.Backend.Tests;

public sealed class DomainTests
{
    [Fact]
    public void ContentSummary_TrimsAndLimitsKeyPointsAndTags()
    {
        var summary = ContentSummary.Create(
            "  Başlık  ",
            "  Özet  ",
            Enumerable.Repeat("ana nokta", 10).Append(" ").ToList(),
            Enumerable.Repeat("etiket", 12).ToList(),
            "  tr  ",
            "  provider  ");

        Assert.Equal("Başlık", summary.Title);
        Assert.Equal("Özet", summary.ShortSummary);
        Assert.Equal(8, summary.KeyPoints.Count);
        Assert.Equal(10, summary.Tags.Count);
        Assert.Equal("tr", summary.Language);
        Assert.Equal("provider", summary.Provider);
    }

    [Fact]
    public void ContentItem_CreateTrimsRawTextAndPreservesMetadata()
    {
        var id = Guid.NewGuid();
        var summary = ContentSummary.Create("Başlık", "Özet", [], [], "tr", "test");

        var item = ContentItem.Create(id, ContentSourceType.Telegram, ContentKind.Video, "  video linki  ", summary);

        Assert.Equal(id, item.Id);
        Assert.Equal(ContentSourceType.Telegram, item.SourceType);
        Assert.Equal(ContentKind.Video, item.ContentKind);
        Assert.Equal("video linki", item.RawText);
        Assert.True(item.CreatedAtUtc <= DateTimeOffset.UtcNow);
    }
}
