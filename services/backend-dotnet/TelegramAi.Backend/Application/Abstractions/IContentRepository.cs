using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Abstractions;

public interface IContentRepository
{
    Task AddAsync(ContentItem contentItem, CancellationToken cancellationToken);
    Task<ContentItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
