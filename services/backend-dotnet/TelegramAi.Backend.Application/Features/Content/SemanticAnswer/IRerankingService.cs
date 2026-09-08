using TelegramAi.Backend.Application.Features.Content.SemanticSearch;

namespace TelegramAi.Backend.Application.Features.Content.SemanticAnswer;

public interface IRerankingService
{
    Task<RerankingSelectionResult> SelectAsync(
        string query,
        IReadOnlyList<SemanticSearchChunkResult> candidates,
        int requestedMaxResults,
        CancellationToken cancellationToken);
}

public sealed record RerankingSelectionResult(
    IReadOnlyList<SemanticSearchChunkResult> Sources,
    IReadOnlyList<RagRerankCandidateDiagnostic> Candidates);
