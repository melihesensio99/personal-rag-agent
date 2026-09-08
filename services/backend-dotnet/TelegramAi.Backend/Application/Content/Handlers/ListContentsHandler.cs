using TelegramAi.Backend.Application.Abstractions;
using TelegramAi.Backend.Application.Common.Pagination;
using TelegramAi.Backend.Application.Content.Queries;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Content.Handlers;

public sealed class ListContentsHandler(IContentRepository contentRepository) : IListContentsHandler
{
    public Task<PagedResult<ContentItem>> HandleAsync(ListContentsQuery query, CancellationToken cancellationToken)
    {
        var normalizedQuery = query with
        {
            Search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
            Page = PaginationDefaults.NormalizePage(query.Page),
            PageSize = PaginationDefaults.NormalizePageSize(query.PageSize)
        };
        return contentRepository.ListAsync(normalizedQuery, cancellationToken);
    }
}
