namespace TelegramAi.Backend.Application.Features.Content.SemanticAnswer;

public interface ISemanticAnswerService
{
    Task<SemanticAnswerResult> AnswerAsync(string query, int maxResults, Guid? contentId, CancellationToken cancellationToken, string? retrievalQuery = null);
    Task<SemanticAnswerDebugResult> AnswerDebugAsync(string query, int maxResults, Guid? contentId, CancellationToken cancellationToken);
}
