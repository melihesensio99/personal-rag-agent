namespace TelegramAi.Backend.Api.Contracts.Search;

public sealed record SemanticSearchResponse(
    string Query,
    IReadOnlyList<SemanticSearchResultResponse> Results);
