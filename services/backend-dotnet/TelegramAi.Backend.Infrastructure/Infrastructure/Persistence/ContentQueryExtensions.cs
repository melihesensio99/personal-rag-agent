using Microsoft.EntityFrameworkCore;
using TelegramAi.Backend.Application.Features.Content.Queries;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Infrastructure.Persistence;

internal static class ContentQueryExtensions
{
    public static IQueryable<ContentItem> ApplyFilters(
        this IQueryable<ContentItem> query,
        ListContentsQuery filter)
    {
        if (filter.SourceType.HasValue)
            query = query.Where(content => content.SourceType == filter.SourceType.Value);
        if (filter.FromUtc.HasValue)
            query = query.Where(content => content.CreatedAtUtc >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue)
            query = query.Where(content => content.CreatedAtUtc < filter.ToUtc.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(content =>
                EF.Functions.ILike(content.RawText, $"%{search}%") ||
                EF.Functions.ILike(content.Summary.Title, $"%{search}%") ||
                EF.Functions.ILike(content.Summary.ShortSummary, $"%{search}%"));
        }
        return query;
    }

    public static IQueryable<ContentItem> ApplyFilters(
        this IQueryable<ContentItem> query,
        SearchContentsQuery filter)
    {
        if (filter.ContentKind.HasValue)
            query = query.Where(content => content.ContentKind == filter.ContentKind.Value);
        if (filter.SourceType.HasValue)
            query = query.Where(content => content.SourceType == filter.SourceType.Value);
        if (filter.FromUtc.HasValue)
            query = query.Where(content => content.CreatedAtUtc >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue)
            query = query.Where(content => content.CreatedAtUtc < filter.ToUtc.Value);
        foreach (var keyword in filter.Keywords.Where(keyword => !string.IsNullOrWhiteSpace(keyword)))
        {
            var currentKeyword = keyword.Trim();
            query = query.Where(content =>
                EF.Functions.ILike(content.RawText, $"%{currentKeyword}%") ||
                EF.Functions.ILike(content.Summary.Title, $"%{currentKeyword}%") ||
                EF.Functions.ILike(content.Summary.ShortSummary, $"%{currentKeyword}%"));
        }
        return query;
    }
}
