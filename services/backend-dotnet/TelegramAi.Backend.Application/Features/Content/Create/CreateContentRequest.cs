using MediatR;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Content.Create;

public sealed record CreateContentCommandRequest(CreateContentCommand Command) : IRequest<ContentItem>;

public sealed class CreateContentCommandHandler(IContentCreationWorkflow workflow)
    : IRequestHandler<CreateContentCommandRequest, ContentItem>
{
    public Task<ContentItem> Handle(CreateContentCommandRequest request, CancellationToken cancellationToken) =>
        workflow.ExecuteAsync(request.Command, cancellationToken);
}
