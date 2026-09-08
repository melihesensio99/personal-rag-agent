using MediatR;
using TelegramAi.Backend.Application.Features.Content.Services;

namespace TelegramAi.Backend.Application.Features.Content.SemanticSearch;

public sealed record SemanticSearchRequest(string Query, int MaxResults, Guid? ContentId)
    : IRequest<IReadOnlyList<SemanticSearchChunkResult>>;

public sealed record SemanticSearchDebugRequest(string Query, int MaxResults, Guid? ContentId)
    : IRequest<SemanticSearchDebugResult>;

public sealed class SemanticSearchRequestHandler(IContentApplicationService service)
    : IRequestHandler<SemanticSearchRequest, IReadOnlyList<SemanticSearchChunkResult>>
{
    public Task<IReadOnlyList<SemanticSearchChunkResult>> Handle(SemanticSearchRequest request, CancellationToken cancellationToken) =>
        service.SemanticSearchChunksAsync(request.Query, request.MaxResults, request.ContentId, cancellationToken);
}

public sealed class SemanticSearchDebugRequestHandler(IContentApplicationService service)
    : IRequestHandler<SemanticSearchDebugRequest, SemanticSearchDebugResult>
{
    public Task<SemanticSearchDebugResult> Handle(SemanticSearchDebugRequest request, CancellationToken cancellationToken) =>
        service.SemanticSearchChunksDebugAsync(request.Query, request.MaxResults, request.ContentId, cancellationToken);
}
