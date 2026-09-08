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
