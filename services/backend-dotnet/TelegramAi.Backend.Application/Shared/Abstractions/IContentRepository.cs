
using TelegramAi.Backend.Domain.Content;
using TelegramAi.Backend.Application.Shared.Common.Pagination;

namespace TelegramAi.Backend.Application.Shared.Abstractions;

public interface IContentRepository
{
    Task AddAsync(ContentItem contentItem, CancellationToken cancellationToken);
    Task AddChunksAsync(IReadOnlyList<ContentChunk> chunks, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<ContentItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ContentChunk>> GetChunksByContentIdAsync(Guid contentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ContentItem>> SearchAsync(FindContentsQuery query, CancellationToken cancellationToken);
    Task<PagedResult<ContentItem>> ListAsync(ListContentsQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<SemanticSearchChunkResult>> SemanticSearchChunksAsync(
        SemanticSearchChunksQuery query,
        CancellationToken cancellationToken);
}
