
using TelegramAi.Backend.Application.Shared.Common.Pagination;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using TelegramAi.Backend.Application.Shared.Abstractions;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Infrastructure.Persistence;

public sealed class EfCoreContentRepository(ApplicationDbContext dbContext) : IContentRepository
{
    public async Task AddAsync(ContentItem contentItem, CancellationToken cancellationToken)
    {
        await dbContext.Contents.AddAsync(contentItem, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddChunksAsync(IReadOnlyList<ContentChunk> chunks, CancellationToken cancellationToken)
    {
        if (chunks.Count == 0)
        {
            return;
        }

        await dbContext.ContentChunks.AddRangeAsync(chunks, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var content = await dbContext.Contents.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (content is null) return false;

        dbContext.Contents.Remove(content);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<ContentItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Contents
            .AsNoTracking()
            .SingleOrDefaultAsync(content => content.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ContentChunk>> GetChunksByContentIdAsync(
        Guid contentId,
        CancellationToken cancellationToken)
    {
        return await dbContext.ContentChunks
            .AsNoTracking()
            .Where(chunk => chunk.ContentItemId == contentId)
            .OrderBy(chunk => chunk.Index)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ContentItem>> SearchAsync(
        FindContentsQuery query,
        CancellationToken cancellationToken)
    {
        var dbQuery = dbContext.Contents
            .AsNoTracking()
            .AsQueryable();

        dbQuery = dbQuery.ApplyFilters(query);

        return await dbQuery
            .OrderByDescending(content => content.CreatedAtUtc)
            .Take(query.MaxResults)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<ContentItem>> ListAsync(ListContentsQuery query, CancellationToken cancellationToken)
    {
        var dbQuery = dbContext.Contents.AsNoTracking().AsQueryable();
        dbQuery = dbQuery.ApplyFilters(query);

        var totalCount = await dbQuery.CountAsync(cancellationToken);
        var items = await dbQuery.OrderByDescending(content => content.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<ContentItem>(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<IReadOnlyList<SemanticSearchChunkResult>> SemanticSearchChunksAsync(
        SemanticSearchChunksQuery query,
        CancellationToken cancellationToken)
    {
        var embedding = new Vector(query.Embedding.ToArray());

        var dbQuery =
            from chunk in dbContext.ContentChunks.AsNoTracking()
            join content in dbContext.Contents.AsNoTracking()
                on chunk.ContentItemId equals content.Id
            where chunk.Embedding != null
            select new
            {
                Chunk = chunk,
                Content = content,
                Distance = chunk.Embedding!.CosineDistance(embedding)
            };

        if (query.ContentId.HasValue)
        {
            var contentId = query.ContentId.Value;
            dbQuery = dbQuery.Where(result => result.Chunk.ContentItemId == contentId);
        }

        return await dbQuery
            .OrderBy(result => result.Distance)
            .Take(query.MaxResults)
            .Select(result => new SemanticSearchChunkResult(
                result.Content.Id,
                result.Chunk.Id,
                result.Content.Summary.Title,
                result.Content.RawText,
                result.Content.SourceType,
                result.Content.ContentKind,
                result.Chunk.Index,
                result.Chunk.Text,
                result.Distance,
                result.Content.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
