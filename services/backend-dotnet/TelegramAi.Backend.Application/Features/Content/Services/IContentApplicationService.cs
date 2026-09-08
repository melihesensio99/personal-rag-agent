using TelegramAi.Backend.Application.Features.Content.Commands;
using TelegramAi.Backend.Application.Features.Content.Queries;
using TelegramAi.Backend.Domain.Content;
using TelegramAi.Backend.Application.Shared.Common.Pagination;

namespace TelegramAi.Backend.Application.Features.Content.Services;

public interface IContentApplicationService
{
    Task<ContentItem> CreateAsync(CreateContentCommand command, CancellationToken cancellationToken);
    Task<ContentItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ContentChunk>> GetChunksByContentIdAsync(Guid contentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ContentItem>> SearchAsync(SearchContentsQuery query, CancellationToken cancellationToken);
    Task<PagedResult<ContentItem>> ListAsync(ListContentsQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<SemanticSearchChunkResult>> SemanticSearchChunksAsync(
        string query,
        int maxResults,
        Guid? contentId,
        CancellationToken cancellationToken);
    Task<SemanticSearchDebugResult> SemanticSearchChunksDebugAsync(
        string query,
        int maxResults,
        Guid? contentId,
        CancellationToken cancellationToken);
    Task<SemanticAnswerResult> SemanticAnswerAsync(
        string query,
        int maxResults,
        Guid? contentId,
        CancellationToken cancellationToken,
        string? retrievalQuery = null);
    Task<SemanticAnswerDebugResult> SemanticAnswerDebugAsync(
        string query,
        int maxResults,
        Guid? contentId,
        CancellationToken cancellationToken);
}
