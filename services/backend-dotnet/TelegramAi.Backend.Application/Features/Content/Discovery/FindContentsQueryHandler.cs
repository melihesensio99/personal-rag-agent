using MediatR;
using TelegramAi.Backend.Application.Shared.Abstractions;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Content.Discovery;

public sealed class FindContentsQueryHandler(IContentRepository repository) : IRequestHandler<FindContentsQuery, IReadOnlyList<ContentItem>>
{
    public Task<IReadOnlyList<ContentItem>> Handle(FindContentsQuery request, CancellationToken cancellationToken) =>
        repository.SearchAsync(request, cancellationToken);
}
