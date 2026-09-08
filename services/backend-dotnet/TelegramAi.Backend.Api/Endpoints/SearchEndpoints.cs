using TelegramAi.Backend.Api.Contracts.Answers;
using TelegramAi.Backend.Api.Contracts.Search;
using TelegramAi.Backend.Application.Features.Content.Services;
using MediatR;
using SemanticSearchMediatorRequest = TelegramAi.Backend.Application.Features.Content.SemanticSearch.SemanticSearchRequest;
using SemanticSearchDebugMediatorRequest = TelegramAi.Backend.Application.Features.Content.SemanticSearch.SemanticSearchDebugRequest;
using SemanticAnswerMediatorRequest = TelegramAi.Backend.Application.Features.Content.SemanticAnswer.SemanticAnswerRequest;
using SemanticAnswerDebugMediatorRequest = TelegramAi.Backend.Application.Features.Content.SemanticAnswer.SemanticAnswerDebugRequest;
using SemanticSearchApiRequest = TelegramAi.Backend.Api.Contracts.Search.SemanticSearchRequest;
using SemanticAnswerApiRequest = TelegramAi.Backend.Api.Contracts.Answers.SemanticAnswerRequest;
using TelegramAi.Backend.Api.Validation;

namespace TelegramAi.Backend.Api;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/search/semantic", SemanticSearchAsync).AddEndpointFilter<FluentValidationEndpointFilter<SemanticSearchApiRequest>>();
        endpoints.MapPost("/api/v1/search/answer", SemanticAnswerAsync).AddEndpointFilter<FluentValidationEndpointFilter<SemanticAnswerApiRequest>>();
        endpoints.MapPost("/api/v1/search/semantic/debug", SemanticSearchDebugAsync).AddEndpointFilter<FluentValidationEndpointFilter<SemanticSearchApiRequest>>();
        endpoints.MapPost("/api/v1/search/answer/debug", SemanticAnswerDebugAsync).AddEndpointFilter<FluentValidationEndpointFilter<SemanticAnswerApiRequest>>();

        return endpoints;
    }

    private static async Task<IResult> SemanticSearchAsync(
        SemanticSearchApiRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = request.Query.Trim();
        var maxResults = Math.Clamp(request.MaxResults, 1, 20);
        var results = await sender.Send(new SemanticSearchMediatorRequest(
            query,
            maxResults,
            request.ContentId), cancellationToken);

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
        SemanticSearchApiRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = request.Query.Trim();
        var maxResults = Math.Clamp(request.MaxResults, 1, 20);
        var result = await sender.Send(new SemanticSearchDebugMediatorRequest(
            query,
            maxResults,
            request.ContentId), cancellationToken);

        return Results.Ok(new SemanticSearchDebugResponse(
            Query: result.Query,
            QueryEmbedding: new SemanticEmbeddingDebugResponse(
                Model: result.EmbeddingModel,
                Dimension: result.EmbeddingDimension,
                Preview: result.QueryEmbeddingPreview),
            Results: result.Results.Select(ToSemanticSearchResultResponse).ToList()));
    }

    private static async Task<IResult> SemanticAnswerAsync(
        SemanticAnswerApiRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = request.Query.Trim();
        var maxResults = Math.Clamp(request.MaxResults, 1, 20);
        var result = await sender.Send(new SemanticAnswerMediatorRequest(
            query,
            maxResults,
            request.ContentId), cancellationToken);

        return Results.Ok(new SemanticAnswerResponse(
            Query: result.Query,
            Answer: result.Answer,
            Provider: result.Provider,
            UsedChunkIndexes: result.UsedChunkIndexes,
            Sources: result.Sources.Select(ToSemanticSearchResultResponse).ToList()));
    }

    private static async Task<IResult> SemanticAnswerDebugAsync(
        SemanticAnswerApiRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = request.Query.Trim();
        var maxResults = Math.Clamp(request.MaxResults, 1, 20);
        var result = await sender.Send(new SemanticAnswerDebugMediatorRequest(
            query,
            maxResults,
            request.ContentId), cancellationToken);

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
            Sources: result.Sources.Select(ToSemanticSearchResultResponse).ToList(),
            MinimumRerankScore: result.MinimumRerankScore,
            RerankCandidates: result.RerankCandidates.Select(candidate => new SemanticAnswerRerankCandidateDebugResponse(
                CandidateIndex: candidate.CandidateIndex,
                ContentId: candidate.ContentId,
                ChunkId: candidate.ChunkId,
                ContentTitle: candidate.ContentTitle,
                ChunkIndex: candidate.ChunkIndex,
                Similarity: candidate.Similarity,
                RerankScore: candidate.RerankScore,
                Accepted: candidate.Accepted,
                Decision: candidate.Decision)).ToList(),
            Timing: new SemanticAnswerTimingDebugResponse(
                RetrievalMilliseconds: result.Timing.RetrievalMilliseconds,
                RerankMilliseconds: result.Timing.RerankMilliseconds,
                AnswerMilliseconds: result.Timing.AnswerMilliseconds,
                TotalMilliseconds: result.Timing.TotalMilliseconds)));
    }

    private static SemanticSearchResultResponse ToSemanticSearchResultResponse(
        Application.Features.Content.SemanticSearch.SemanticSearchChunkResult result)
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
