using MediatR;

namespace TelegramAi.Backend.Application.Features.Content.SemanticSearch;

public sealed record SemanticSearchQuery(string Query, int MaxResults, Guid? ContentId)
    : IRequest<IReadOnlyList<SemanticSearchChunkResult>>;

public sealed class SemanticSearchQueryHandler(ISemanticSearchService service)
    : IRequestHandler<SemanticSearchQuery, IReadOnlyList<SemanticSearchChunkResult>>
{
    public Task<IReadOnlyList<SemanticSearchChunkResult>> Handle(SemanticSearchQuery request, CancellationToken cancellationToken) =>
        service.SearchAsync(request.Query, request.MaxResults, request.ContentId, cancellationToken);
}
