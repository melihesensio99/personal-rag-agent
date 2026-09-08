using TelegramAi.Backend.Application.Features.Content.Commands;
using TelegramAi.Backend.Application.Features.Content.Queries;
using TelegramAi.Backend.Application.Shared.Common.Pagination;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Content.Handlers;

public interface ICreateContentHandler { Task<ContentItem> HandleAsync(CreateContentCommand command, CancellationToken cancellationToken); }
public interface IGetContentHandler { Task<ContentItem?> HandleAsync(Guid id, CancellationToken cancellationToken); }
public interface IGetContentChunksHandler { Task<IReadOnlyList<ContentChunk>> HandleAsync(Guid contentId, CancellationToken cancellationToken); }
public interface IListContentHandler { Task<PagedResult<ContentItem>> HandleAsync(ListContentsQuery query, CancellationToken cancellationToken); }
