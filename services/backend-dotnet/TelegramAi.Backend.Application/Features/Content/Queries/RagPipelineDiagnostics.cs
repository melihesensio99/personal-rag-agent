namespace TelegramAi.Backend.Application.Features.Content.Queries;

public sealed record RagRerankCandidateDiagnostic(
    int CandidateIndex,
    Guid ContentId,
    Guid ChunkId,
    string ContentTitle,
    int ChunkIndex,
    double Similarity,
    double RerankScore,
    bool Accepted,
    string Decision);

public sealed record RagPipelineTiming(
    long RetrievalMilliseconds,
    long RerankMilliseconds,
    long AnswerMilliseconds,
    long TotalMilliseconds);

internal sealed record RerankSelectionResult(
    IReadOnlyList<SemanticSearchChunkResult> Sources,
    IReadOnlyList<RagRerankCandidateDiagnostic> Candidates);
