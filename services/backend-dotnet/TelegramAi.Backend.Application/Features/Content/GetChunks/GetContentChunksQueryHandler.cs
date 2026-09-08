using MediatR;
using TelegramAi.Backend.Application.Shared.Abstractions;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Content.GetChunks;

public sealed class GetContentChunksQueryHandler(IContentRepository repository)
    : IRequestHandler<GetContentChunksQuery, IReadOnlyList<ContentChunk>>
{
    public Task<IReadOnlyList<ContentChunk>> Handle(GetContentChunksQuery request, CancellationToken cancellationToken) => repository.GetChunksByContentIdAsync(request.Id, cancellationToken);
}
