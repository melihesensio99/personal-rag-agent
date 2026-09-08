using TelegramAi.Backend.Application.Shared.Abstractions;
using TelegramAi.Backend.Application.Shared.Common.Pagination;
using TelegramAi.Backend.Application.Features.Content.Queries;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Content.Handlers;

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
