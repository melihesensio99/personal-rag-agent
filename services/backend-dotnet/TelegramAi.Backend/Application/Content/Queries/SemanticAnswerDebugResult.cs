using TelegramAi.Backend.Api.Contracts.Answers;

namespace TelegramAi.Backend.Application.Content.Queries;

public sealed record SemanticAnswerDebugResult(
    string Query,
    string EmbeddingModel,
    int EmbeddingDimension,
    IReadOnlyList<float> QueryEmbeddingPreview,
    string AnswerProvider,
    string Answer,
    IReadOnlyList<int> UsedChunkIndexes,
    IReadOnlyList<CreateAnswerChunkRequest> ContextChunksSentToLlm,
    IReadOnlyList<SemanticSearchChunkResult> Sources);
