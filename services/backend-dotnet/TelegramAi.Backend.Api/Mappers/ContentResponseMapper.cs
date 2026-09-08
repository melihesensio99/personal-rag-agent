using TelegramAi.Backend.Api.Contracts.Content;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Api.Mappers;

public static class ContentResponseMapper
{
    public static ContentResponse Map(ContentItem contentItem)
    {
        return new ContentResponse(
            Id: contentItem.Id,
            SourceType: contentItem.SourceType.ToString(),
            RawText: contentItem.RawText,
            CreatedAtUtc: contentItem.CreatedAtUtc,
            Summary: new ContentSummaryResponse(
                Title: contentItem.Summary.Title,
                ShortSummary: contentItem.Summary.ShortSummary,
                KeyPoints: contentItem.Summary.KeyPoints,
                Tags: contentItem.Summary.Tags,
                Language: contentItem.Summary.Language,
                Provider: contentItem.Summary.Provider));
    }
}
