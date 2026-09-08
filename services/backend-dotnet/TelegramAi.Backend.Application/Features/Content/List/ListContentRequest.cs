using MediatR;
using TelegramAi.Backend.Application.Shared.Abstractions;
using TelegramAi.Backend.Application.Shared.Common.Pagination;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Content.List;

public sealed record ListContentQuery(ListContentsQuery Query) : IRequest<PagedResult<ContentItem>>;

public sealed class ListContentQueryHandler(IContentRepository repository)
    : IRequestHandler<ListContentQuery, PagedResult<ContentItem>>
{
    public Task<PagedResult<ContentItem>> Handle(
        ListContentQuery request,
        CancellationToken cancellationToken)
    {
        var query = request.Query with
        {
            Search = string.IsNullOrWhiteSpace(request.Query.Search) ? null : request.Query.Search.Trim(),
            Page = PaginationDefaults.NormalizePage(request.Query.Page),
            PageSize = PaginationDefaults.NormalizePageSize(request.Query.PageSize)
        };

        return repository.ListAsync(query, cancellationToken);
    }
}
