using TelegramAi.Backend.Api.Contracts.Search;

namespace TelegramAi.Backend.Api.Contracts.Answers;

public sealed record SemanticAnswerDebugResponse(
    string Query,
    string Answer,
    string Provider,
    SemanticEmbeddingDebugResponse QueryEmbedding,
    IReadOnlyList<int> UsedChunkIndexes,
    IReadOnlyList<SemanticAnswerContextChunkDebugResponse> ContextChunksSentToLlm,
    IReadOnlyList<SemanticSearchResultResponse> Sources,
    double MinimumRerankScore,
    IReadOnlyList<SemanticAnswerRerankCandidateDebugResponse> RerankCandidates,
    SemanticAnswerTimingDebugResponse Timing);

public sealed record SemanticAnswerContextChunkDebugResponse(
    int Index,
    Guid ContentId,
    Guid ChunkId,
    string ContentTitle,
    string ContentUrl,
    string SourceType,
    string ContentKind,
    int ChunkIndex,
    double Distance,
    double Similarity,
    int TextLength,
    string TextPreview);

public sealed record SemanticAnswerRerankCandidateDebugResponse(
    int CandidateIndex,
    Guid ContentId,
    Guid ChunkId,
    string ContentTitle,
    int ChunkIndex,
    double Similarity,
    double RerankScore,
    bool Accepted,
    string Decision);

public sealed record SemanticAnswerTimingDebugResponse(
    long RetrievalMilliseconds,
    long RerankMilliseconds,
    long AnswerMilliseconds,
    long TotalMilliseconds);
