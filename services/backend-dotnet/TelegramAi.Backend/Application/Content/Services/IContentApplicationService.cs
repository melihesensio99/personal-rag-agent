using TelegramAi.Backend.Application.Content.Commands;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Content.Services;

public interface IContentApplicationService
{
    Task<ContentItem> CreateAsync(CreateContentCommand command, CancellationToken cancellationToken);
    Task<ContentItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
