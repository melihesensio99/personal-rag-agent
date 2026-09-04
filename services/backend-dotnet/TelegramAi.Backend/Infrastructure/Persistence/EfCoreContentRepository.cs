using TelegramAi.Backend.Application.Content.Queries;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
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

    public async Task AddChunksAsync(IReadOnlyList<ContentChunk> chunks, CancellationToken cancellationToken)
    {
        if (chunks.Count == 0)
        {
            return;
        }

        await dbContext.ContentChunks.AddRangeAsync(chunks, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
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
        SearchContentsQuery query,
        CancellationToken cancellationToken)
    {
        var dbQuery = dbContext.Contents
            .AsNoTracking()
            .AsQueryable();

        if (query.ContentKind.HasValue)
        {
            dbQuery = dbQuery.Where(content => content.ContentKind == query.ContentKind.Value);
        }

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

    public async Task<IReadOnlyList<SemanticSearchChunkResult>> SemanticSearchChunksAsync(
        SemanticSearchChunksQuery query,
        CancellationToken cancellationToken)
    {
        var embedding = new Vector(query.Embedding.ToArray());

        var dbQuery = dbContext.ContentChunks
            .AsNoTracking()
            .Where(chunk => chunk.Embedding != null);

        if (query.ContentId.HasValue)
        {
            var contentId = query.ContentId.Value;
            dbQuery = dbQuery.Where(chunk => chunk.ContentItemId == contentId);
        }

        var topChunks = await dbQuery
            .Select(chunk => new
            {
                Chunk = chunk,
                Distance = chunk.Embedding!.CosineDistance(embedding),
            })
            .OrderBy(result => result.Distance)
            .Take(query.MaxResults)
            .ToListAsync(cancellationToken);

        var contentIds = topChunks
            .Select(result => result.Chunk.ContentItemId)
            .Distinct()
            .ToList();

        var contentsById = await dbContext.Contents
            .AsNoTracking()
            .Where(content => contentIds.Contains(content.Id))
            .ToDictionaryAsync(content => content.Id, cancellationToken);

        return topChunks
            .Where(result => contentsById.ContainsKey(result.Chunk.ContentItemId))
            .Select(result =>
            {
                var content = contentsById[result.Chunk.ContentItemId];

                return new SemanticSearchChunkResult(
                    content.Id,
                    result.Chunk.Id,
                    content.Summary.Title,
                    content.RawText,
                    content.SourceType,
                    content.ContentKind,
                    result.Chunk.Index,
                    result.Chunk.Text,
                    result.Distance,
                    content.CreatedAtUtc);
            })
            .ToList();
    }
}
