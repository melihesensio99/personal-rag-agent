using TelegramAi.Backend.Api.Contracts.Search;

namespace TelegramAi.Backend.Api.Contracts.Answers;

public sealed record SemanticAnswerDebugResponse(
    string Query,
    string Answer,
    string Provider,
    SemanticEmbeddingDebugResponse QueryEmbedding,
    IReadOnlyList<int> UsedChunkIndexes,
    IReadOnlyList<SemanticAnswerContextChunkDebugResponse> ContextChunksSentToLlm,
    IReadOnlyList<SemanticSearchResultResponse> Sources);

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
