using TelegramAi.Backend.Application.Shared.Common.Pagination;
using TelegramAi.Backend.Application.Features.Content.Queries;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Content.Handlers;

public interface IListContentsHandler
{
    Task<PagedResult<ContentItem>> HandleAsync(ListContentsQuery query, CancellationToken cancellationToken);
}
