using Microsoft.EntityFrameworkCore;
using TelegramAi.Backend.Application.Abstractions;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Infrastructure.Persistence;

public sealed class EfCoreContentRepository(ApplicationDbContext dbContext) : IContentRepository
{
    public async Task AddAsync(ContentItem contentItem, CancellationToken cancellationToken)
    {
        await dbContext.Contents.AddAsync(contentItem, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<ContentItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Contents
            .AsNoTracking()
            .SingleOrDefaultAsync(content => content.Id == id, cancellationToken);
    }
}
