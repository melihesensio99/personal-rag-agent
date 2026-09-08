namespace TelegramAi.Backend.Application.Features.Content.SemanticSearch;

public interface ISemanticSearchService
{
    Task<IReadOnlyList<SemanticSearchChunkResult>> SearchAsync(string query, int maxResults, Guid? contentId, CancellationToken cancellationToken);
    Task<SemanticSearchDebugResult> SearchDebugAsync(string query, int maxResults, Guid? contentId, CancellationToken cancellationToken);
}
