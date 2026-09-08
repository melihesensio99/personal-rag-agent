using MediatR;
using TelegramAi.Backend.Application.Shared.Abstractions;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Content.Discovery;

public sealed record FindContentsRequest(FindContentsQuery Query) : IRequest<IReadOnlyList<ContentItem>>;

public sealed class FindContentsRequestHandler(IContentRepository repository)
    : IRequestHandler<FindContentsRequest, IReadOnlyList<ContentItem>>
{
    public Task<IReadOnlyList<ContentItem>> Handle(FindContentsRequest request, CancellationToken cancellationToken) =>
        repository.SearchAsync(request.Query, cancellationToken);
}
