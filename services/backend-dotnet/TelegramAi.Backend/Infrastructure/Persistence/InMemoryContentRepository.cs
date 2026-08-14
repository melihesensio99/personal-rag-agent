using System.Collections.Concurrent;
using TelegramAi.Backend.Application.Abstractions;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Infrastructure.Persistence;

public sealed class InMemoryContentRepository : IContentRepository
{
    private readonly ConcurrentDictionary<Guid, ContentItem> _contents = new();

    public Task AddAsync(ContentItem contentItem, CancellationToken cancellationToken)
    {
        _contents[contentItem.Id] = contentItem;
        return Task.CompletedTask;
    }

    public Task<ContentItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        _contents.TryGetValue(id, out var contentItem);
        return Task.FromResult(contentItem);
    }
}
