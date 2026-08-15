using TelegramAi.Backend.Application.Content.Queries;
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

    public async Task<IReadOnlyList<ContentItem>> SearchAsync(
        SearchContentsQuery query,
        CancellationToken cancellationToken)
    {
        var dbQuery = dbContext.Contents
            .AsNoTracking()
            .AsQueryable();

        if (query.SourceType.HasValue)
        {
            dbQuery = dbQuery.Where(content => content.SourceType == query.SourceType.Value);
        }

        if (query.FromUtc.HasValue)
        {
            dbQuery = dbQuery.Where(content => content.CreatedAtUtc >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            dbQuery = dbQuery.Where(content => content.CreatedAtUtc < query.ToUtc.Value);
        }

        foreach (var keyword in query.Keywords)
        {
            var currentKeyword = keyword;
            dbQuery = dbQuery.Where(content =>
                EF.Functions.ILike(content.RawText, $"%{currentKeyword}%") ||
                EF.Functions.ILike(content.Summary.Title, $"%{currentKeyword}%") ||
                EF.Functions.ILike(content.Summary.ShortSummary, $"%{currentKeyword}%"));
        }

        return await dbQuery
            .OrderByDescending(content => content.CreatedAtUtc)
            .Take(query.MaxResults)
            .ToListAsync(cancellationToken);
    }
}
