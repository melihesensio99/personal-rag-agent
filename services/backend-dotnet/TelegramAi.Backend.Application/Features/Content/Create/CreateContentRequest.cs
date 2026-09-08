using MediatR;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Content.Create;

public sealed record CreateContentRequest(CreateContentCommand Command) : IRequest<ContentItem>;

public sealed class CreateContentRequestHandler(IContentCreationWorkflow workflow)
    : IRequestHandler<CreateContentRequest, ContentItem>
{
    public Task<ContentItem> Handle(CreateContentRequest request, CancellationToken cancellationToken) =>
        workflow.ExecuteAsync(request.Command, cancellationToken);
}
