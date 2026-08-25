using TelegramAi.Backend.Api.Contracts.Answers;
using TelegramAi.Backend.Api.Contracts.Search;
using TelegramAi.Backend.Application.Content.Services;

namespace TelegramAi.Backend.Api;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/search/semantic", SemanticSearchAsync);
        endpoints.MapPost("/api/v1/search/answer", SemanticAnswerAsync);

        return endpoints;
    }

    private static async Task<IResult> SemanticSearchAsync(
        SemanticSearchRequest request,
        IContentApplicationService contentApplicationService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return Results.BadRequest(new { error = "Query cannot be empty." });
        }

        var query = request.Query.Trim();
        var maxResults = Math.Clamp(request.MaxResults, 1, 20);
        var results = await contentApplicationService.SemanticSearchChunksAsync(
            query,
            maxResults,
            request.ContentId,
            cancellationToken);

        return Results.Ok(new SemanticSearchResponse(
            Query: query,
            Results: results.Select(result => new SemanticSearchResultResponse(
                ContentId: result.ContentId,
                ChunkId: result.ChunkId,
                ContentTitle: result.ContentTitle,
                ContentUrl: result.ContentUrl,
                SourceType: result.SourceType.ToString(),
                ContentKind: result.ContentKind.ToString(),
                ChunkIndex: result.ChunkIndex,
                ChunkText: result.ChunkText,
                Distance: result.Distance,
                Similarity: 1 - result.Distance,
                ContentCreatedAtUtc: result.ContentCreatedAtUtc)).ToList()));
    }

    private static async Task<IResult> SemanticAnswerAsync(
        SemanticAnswerRequest request,
        IContentApplicationService contentApplicationService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return Results.BadRequest(new { error = "Query cannot be empty." });
        }

        var query = request.Query.Trim();
        var maxResults = Math.Clamp(request.MaxResults, 1, 20);
        var result = await contentApplicationService.SemanticAnswerAsync(
            query,
            maxResults,
            request.ContentId,
            cancellationToken);

        return Results.Ok(new SemanticAnswerResponse(
            Query: result.Query,
            Answer: result.Answer,
            Provider: result.Provider,
            UsedChunkIndexes: result.UsedChunkIndexes,
            Sources: result.Sources.Select(source => new SemanticSearchResultResponse(
                ContentId: source.ContentId,
                ChunkId: source.ChunkId,
                ContentTitle: source.ContentTitle,
                ContentUrl: source.ContentUrl,
                SourceType: source.SourceType.ToString(),
                ContentKind: source.ContentKind.ToString(),
                ChunkIndex: source.ChunkIndex,
                ChunkText: source.ChunkText,
                Distance: source.Distance,
                Similarity: 1 - source.Distance,
                ContentCreatedAtUtc: source.ContentCreatedAtUtc)).ToList()));
    }
}
