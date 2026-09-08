using MediatR;
using TelegramAi.Backend.Application.Shared.Abstractions;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Content.GetChunks;

public sealed record GetContentChunksRequest(Guid Id) : IRequest<IReadOnlyList<ContentChunk>>;

public sealed class GetContentChunksRequestHandler(IContentRepository repository)
    : IRequestHandler<GetContentChunksRequest, IReadOnlyList<ContentChunk>>
{
    public Task<IReadOnlyList<ContentChunk>> Handle(
        GetContentChunksRequest request,
        CancellationToken cancellationToken) =>
        repository.GetChunksByContentIdAsync(request.Id, cancellationToken);
}
