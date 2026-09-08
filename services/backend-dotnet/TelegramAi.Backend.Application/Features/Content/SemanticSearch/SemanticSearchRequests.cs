using MediatR;
using TelegramAi.Backend.Application.Shared.Abstractions;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Content.SemanticSearch;

public sealed record SearchContentsRequest(SearchContentsQuery Query) : IRequest<IReadOnlyList<ContentItem>>;

public sealed class SearchContentsRequestHandler(IContentRepository repository)
    : IRequestHandler<SearchContentsRequest, IReadOnlyList<ContentItem>>
{
    public Task<IReadOnlyList<ContentItem>> Handle(SearchContentsRequest request, CancellationToken cancellationToken) =>
        repository.SearchAsync(request.Query, cancellationToken);
}

public sealed record SemanticSearchQuery(string Query, int MaxResults, Guid? ContentId)
    : IRequest<IReadOnlyList<SemanticSearchChunkResult>>;

public sealed record SemanticSearchDebugQuery(string Query, int MaxResults, Guid? ContentId)
    : IRequest<SemanticSearchDebugResult>;

public sealed class SemanticSearchQueryHandler(ISemanticSearchService service)
    : IRequestHandler<SemanticSearchQuery, IReadOnlyList<SemanticSearchChunkResult>>
{
    public Task<IReadOnlyList<SemanticSearchChunkResult>> Handle(SemanticSearchQuery request, CancellationToken cancellationToken) =>
        service.SearchAsync(request.Query, request.MaxResults, request.ContentId, cancellationToken);
}

public sealed class SemanticSearchDebugQueryHandler(ISemanticSearchService service)
    : IRequestHandler<SemanticSearchDebugQuery, SemanticSearchDebugResult>
{
    public Task<SemanticSearchDebugResult> Handle(SemanticSearchDebugQuery request, CancellationToken cancellationToken) =>
        service.SearchDebugAsync(request.Query, request.MaxResults, request.ContentId, cancellationToken);
}
