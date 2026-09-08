using TelegramAi.Backend.Api.Contracts.Search;

namespace TelegramAi.Backend.Api.Contracts.Answers;

public sealed record SemanticAnswerResponse(
    string Query,
    string Answer,
    string Provider,
    IReadOnlyList<int> UsedChunkIndexes,
    IReadOnlyList<SemanticSearchResultResponse> Sources);
