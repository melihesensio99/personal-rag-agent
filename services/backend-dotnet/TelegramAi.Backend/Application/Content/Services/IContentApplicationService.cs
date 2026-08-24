using TelegramAi.Backend.Application.Content.Commands;
using TelegramAi.Backend.Application.Content.Queries;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Content.Services;

public interface IContentApplicationService
{
    Task<ContentItem> CreateAsync(CreateContentCommand command, CancellationToken cancellationToken);
    Task<ContentItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ContentChunk>> GetChunksByContentIdAsync(Guid contentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ContentItem>> SearchAsync(SearchContentsQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<SemanticSearchChunkResult>> SemanticSearchChunksAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken);
}
