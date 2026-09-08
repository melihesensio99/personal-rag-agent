using MediatR;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Content.Create;

public sealed class CreateContentCommandHandler(IContentCreationWorkflow workflow)
    : IRequestHandler<CreateContentCommand, ContentItem>
{
    public Task<ContentItem> Handle(CreateContentCommand request, CancellationToken cancellationToken) =>
        workflow.ExecuteAsync(request, cancellationToken);
}
