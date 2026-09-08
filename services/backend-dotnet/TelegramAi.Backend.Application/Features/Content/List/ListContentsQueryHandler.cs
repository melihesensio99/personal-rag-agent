using MediatR;
using TelegramAi.Backend.Application.Shared.Abstractions;
using TelegramAi.Backend.Application.Shared.Common.Pagination;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Content.List;

public sealed class ListContentsQueryHandler(IContentRepository repository) : IRequestHandler<ListContentsQuery, PagedResult<ContentItem>>
{
    public Task<PagedResult<ContentItem>> Handle(ListContentsQuery request, CancellationToken cancellationToken) =>
        repository.ListAsync(request with
        {
            Search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim(),
            Page = PaginationDefaults.NormalizePage(request.Page),
            PageSize = PaginationDefaults.NormalizePageSize(request.PageSize)
        }, cancellationToken);
}
