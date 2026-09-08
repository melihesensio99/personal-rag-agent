using TelegramAi.Backend.Application.Common.Pagination;
using TelegramAi.Backend.Application.Content.Queries;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Content.Handlers;

public interface IListContentsHandler
{
    Task<PagedResult<ContentItem>> HandleAsync(ListContentsQuery query, CancellationToken cancellationToken);
}
