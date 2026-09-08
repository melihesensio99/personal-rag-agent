using TelegramAi.Backend.Application.Features.Content.Commands;
using TelegramAi.Backend.Application.Features.Content.Queries;
using TelegramAi.Backend.Application.Features.Content.Services;
using TelegramAi.Backend.Application.Shared.Common.Pagination;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Content.Handlers;

public sealed class CreateContentHandler(IContentApplicationService service) : ICreateContentHandler { public Task<ContentItem> HandleAsync(CreateContentCommand command, CancellationToken cancellationToken) => service.CreateAsync(command, cancellationToken); }
public sealed class GetContentHandler(IContentApplicationService service) : IGetContentHandler { public Task<ContentItem?> HandleAsync(Guid id, CancellationToken cancellationToken) => service.GetByIdAsync(id, cancellationToken); }
public sealed class GetContentChunksHandler(IContentApplicationService service) : IGetContentChunksHandler { public Task<IReadOnlyList<ContentChunk>> HandleAsync(Guid id, CancellationToken cancellationToken) => service.GetChunksByContentIdAsync(id, cancellationToken); }
public sealed class ListContentHandler(IContentApplicationService service) : IListContentHandler { public Task<PagedResult<ContentItem>> HandleAsync(ListContentsQuery query, CancellationToken cancellationToken) => service.ListAsync(query, cancellationToken); }
