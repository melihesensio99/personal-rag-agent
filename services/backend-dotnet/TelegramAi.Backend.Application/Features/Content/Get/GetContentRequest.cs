using MediatR;
using TelegramAi.Backend.Application.Shared.Abstractions;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Content.Get;

public sealed record GetContentRequest(Guid Id) : IRequest<ContentItem?>;

public sealed class GetContentRequestHandler(IContentRepository repository)
    : IRequestHandler<GetContentRequest, ContentItem?>
{
    public Task<ContentItem?> Handle(GetContentRequest request, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(request.Id, cancellationToken);
}
