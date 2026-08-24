namespace TelegramAi.Backend.Api.Contracts.Search;

public sealed record SemanticSearchRequest(
    string Query,
    int MaxResults = 5);
