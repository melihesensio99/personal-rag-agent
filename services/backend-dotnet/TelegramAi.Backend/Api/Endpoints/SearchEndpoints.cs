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
        endpoints.MapPost("/api/v1/search/semantic/debug", SemanticSearchDebugAsync);
        endpoints.MapPost("/api/v1/search/answer/debug", SemanticAnswerDebugAsync);

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

    private static async Task<IResult> SemanticSearchDebugAsync(
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
        var result = await contentApplicationService.SemanticSearchChunksDebugAsync(
            query,
            maxResults,
            request.ContentId,
            cancellationToken);

        return Results.Ok(new SemanticSearchDebugResponse(
            Query: result.Query,
            QueryEmbedding: new SemanticEmbeddingDebugResponse(
                Model: result.EmbeddingModel,
                Dimension: result.EmbeddingDimension,
                Preview: result.QueryEmbeddingPreview),
            Results: result.Results.Select(ToSemanticSearchResultResponse).ToList()));
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
            Sources: result.Sources.Select(ToSemanticSearchResultResponse).ToList()));
    }

    private static async Task<IResult> SemanticAnswerDebugAsync(
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
        var result = await contentApplicationService.SemanticAnswerDebugAsync(
            query,
            maxResults,
            request.ContentId,
            cancellationToken);

        return Results.Ok(new SemanticAnswerDebugResponse(
            Query: result.Query,
            Answer: result.Answer,
            Provider: result.AnswerProvider,
            QueryEmbedding: new SemanticEmbeddingDebugResponse(
                Model: result.EmbeddingModel,
                Dimension: result.EmbeddingDimension,
                Preview: result.QueryEmbeddingPreview),
            UsedChunkIndexes: result.UsedChunkIndexes,
            ContextChunksSentToLlm: result.ContextChunksSentToLlm.Select(chunk => new SemanticAnswerContextChunkDebugResponse(
                Index: chunk.Index,
                ContentId: Guid.ParseExact(chunk.ContentId, "N"),
                ChunkId: Guid.ParseExact(chunk.ChunkId, "N"),
                ContentTitle: chunk.ContentTitle,
                ContentUrl: chunk.ContentUrl,
                SourceType: chunk.SourceType,
                ContentKind: chunk.ContentKind,
                ChunkIndex: chunk.ChunkIndex,
                Distance: chunk.Distance,
                Similarity: chunk.Similarity,
                TextLength: chunk.Text.Length,
                TextPreview: BuildPreview(chunk.Text))).ToList(),
            Sources: result.Sources.Select(ToSemanticSearchResultResponse).ToList()));
    }

    private static SemanticSearchResultResponse ToSemanticSearchResultResponse(
        Application.Content.Queries.SemanticSearchChunkResult result)
    {
        return new SemanticSearchResultResponse(
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
            ContentCreatedAtUtc: result.ContentCreatedAtUtc);
    }

    private static string BuildPreview(string text)
    {
        const int maxPreviewLength = 280;
        var normalized = string.Join(" ", text.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));

        return normalized.Length <= maxPreviewLength
            ? normalized
            : $"{normalized[..maxPreviewLength]}...";
    }
}
