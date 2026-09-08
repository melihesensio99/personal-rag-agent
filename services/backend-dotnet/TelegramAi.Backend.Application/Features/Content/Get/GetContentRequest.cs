using MediatR;
using TelegramAi.Backend.Application.Shared.Abstractions;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Content.Get;

public sealed record GetContentQuery(Guid Id) : IRequest<ContentItem?>;

public sealed class GetContentQueryHandler(IContentRepository repository)
    : IRequestHandler<GetContentQuery, ContentItem?>
{
    public Task<ContentItem?> Handle(GetContentQuery request, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(request.Id, cancellationToken);
}
