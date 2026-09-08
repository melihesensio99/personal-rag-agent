using TelegramAi.Backend.Application.Features.Content.Create;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Content.Create;

public interface IContentCreationWorkflow
{
    Task<ContentItem> ExecuteAsync(CreateContentCommand command, CancellationToken cancellationToken);
}
