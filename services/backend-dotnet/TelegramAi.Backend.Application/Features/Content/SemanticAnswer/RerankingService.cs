using Microsoft.Extensions.Options;
using TelegramAi.Backend.Application.Contracts.Reranking;
using TelegramAi.Backend.Application.Features.Content.SemanticSearch;
using TelegramAi.Backend.Application.Shared.Abstractions;

namespace TelegramAi.Backend.Application.Features.Content.SemanticAnswer;

public sealed class RerankingService(
    IAiServiceClient aiServiceClient,
    IOptions<AnswerRetrievalOptions> options) : IRerankingService
{
    private const int MaxChunksPerContent = 3;
    private const int MaxAnswerChunks = 8;
    private const int MaxAnswerContextCharacters = 12_000;

    public async Task<RerankingSelectionResult> SelectAsync(
        string query,
        IReadOnlyList<SemanticSearchChunkResult> candidates,
        int requestedMaxResults,
        CancellationToken cancellationToken)
    {
        if (candidates.Count == 0) return new([], []);

        var response = await aiServiceClient.RerankAsync(new RerankInput(
            query,
            candidates.Select((candidate, index) => new RerankDocumentInput(index, candidate.ChunkText)).ToList()), cancellationToken);
        var scores = response.Scores.ToDictionary(x => x.Index, x => x.Score);
        var minimumScore = options.Value.MinimumRerankScore;
        var scored = candidates.Select((candidate, index) => new ScoredCandidate(index, candidate,
            scores.GetValueOrDefault(index), scores.ContainsKey(index))).ToList();
        var passed = scored.Where(x => x.HasScore && x.Score >= minimumScore).OrderByDescending(x => x.Score).ToList();
        var selected = SelectSources(passed, requestedMaxResults);
        var selectedIds = selected.Select(x => x.ChunkId).ToHashSet();
        var diagnostics = scored.Select(x => new RagRerankCandidateDiagnostic(
            x.Index, x.Candidate.ContentId, x.Candidate.ChunkId, x.Candidate.ContentTitle,
            x.Candidate.ChunkIndex, Math.Max(0, 1 - x.Candidate.Distance), x.Score,
            selectedIds.Contains(x.Candidate.ChunkId), ResolveDecision(x, minimumScore, selectedIds)))
            .OrderByDescending(x => x.RerankScore).ToList();
        return new(selected, diagnostics);
    }

    private static IReadOnlyList<SemanticSearchChunkResult> SelectSources(
        IReadOnlyList<ScoredCandidate> candidates, int requestedMaxResults)
    {
        var limit = Math.Clamp(requestedMaxResults, 1, MaxAnswerChunks);
        var groups = candidates.GroupBy(x => x.Candidate.ContentId)
            .Select(g => g.OrderByDescending(x => x.Score).Take(MaxChunksPerContent).ToList())
            .OrderByDescending(g => g[0].Score).ToList();
        var selected = new List<SemanticSearchChunkResult>();
        var characters = 0;
        for (var round = 0; round < MaxChunksPerContent && selected.Count < limit; round++)
            foreach (var group in groups)
            {
                if (round >= group.Count || selected.Count >= limit) continue;
                var candidate = group[round].Candidate;
                if (selected.Count > 0 && characters + candidate.ChunkText.Length > MaxAnswerContextCharacters) continue;
                selected.Add(candidate);
                characters += candidate.ChunkText.Length;
            }
        return selected;
    }

    private static string ResolveDecision(ScoredCandidate candidate, double minimumScore, IReadOnlySet<Guid> selectedIds) =>
        !candidate.HasScore ? "missing_rerank_score" : candidate.Score < minimumScore ? "below_rerank_threshold" :
        selectedIds.Contains(candidate.Candidate.ChunkId) ? "selected_for_answer" : "excluded_by_context_limits";

    private sealed record ScoredCandidate(int Index, SemanticSearchChunkResult Candidate, double Score, bool HasScore);
}
