using TelegramAi.Backend.Application.Contracts.Answers;

namespace TelegramAi.Backend.Application.Content.Queries;

public sealed record SemanticAnswerDebugResult(
    string Query,
    string EmbeddingModel,
    int EmbeddingDimension,
    IReadOnlyList<float> QueryEmbeddingPreview,
    string AnswerProvider,
    string Answer,
    IReadOnlyList<int> UsedChunkIndexes,
    IReadOnlyList<AnswerChunkInput> ContextChunksSentToLlm,
    IReadOnlyList<SemanticSearchChunkResult> Sources,
    double MinimumRerankScore,
    IReadOnlyList<RagRerankCandidateDiagnostic> RerankCandidates,
    RagPipelineTiming Timing);
