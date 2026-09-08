using TelegramAi.Backend.Api.Contracts.Content;
using TelegramAi.Backend.Api.Mappers;

using TelegramAi.Backend.Application.Features.Content.Services;
using TelegramAi.Backend.Domain.Content;
using Microsoft.AspNetCore.Mvc;
using TelegramAi.Backend.Api.Contracts.Common;

using TelegramAi.Backend.Api.Validation;
using TelegramAi.Backend.Application.Features.Content.Create;
using TelegramAi.Backend.Application.Features.Content.List;
using CreateContentMediatorRequest = TelegramAi.Backend.Application.Features.Content.Create.CreateContentRequest;
using CreateContentApiRequest = TelegramAi.Backend.Api.Contracts.Content.CreateContentRequest;
using GetContentMediatorRequest = TelegramAi.Backend.Application.Features.Content.Get.GetContentRequest;
using GetContentChunksMediatorRequest = TelegramAi.Backend.Application.Features.Content.GetChunks.GetContentChunksRequest;
using ListContentMediatorRequest = TelegramAi.Backend.Application.Features.Content.List.ListContentRequest;
using MediatR;

namespace TelegramAi.Backend.Api;

public static class ContentEndpoints
{
    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/contents", CreateContentAsync)
            .AddEndpointFilter<FluentValidationEndpointFilter<CreateContentApiRequest>>();
        endpoints.MapGet("/api/v1/contents", ListContentsAsync)
            .AddEndpointFilter<FluentValidationEndpointFilter<ListContentsRequest>>();
        endpoints.MapGet("/api/v1/contents/{id:guid}", GetContentByIdAsync);
        endpoints.MapGet("/api/v1/contents/{id:guid}/chunks", GetContentChunksByIdAsync);

        return endpoints;
    }

    private static async Task<IResult> ListContentsAsync(
        [AsParameters] ListContentsRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        ContentSourceType? sourceType = null;
        if (!string.IsNullOrWhiteSpace(request.SourceType))
        {
            if (!ContentSourceTypeParser.TryParse(request.SourceType, out var parsedSourceType))
                return Results.BadRequest(new { error = "Invalid sourceType." });
            sourceType = parsedSourceType;
        }
        if (request.FromUtc.HasValue && request.ToUtc.HasValue && request.FromUtc >= request.ToUtc)
            return Results.BadRequest(new { error = "fromUtc must be earlier than toUtc." });

        var result = await sender.Send(new ListContentMediatorRequest(new ListContentsQuery(
            request.Search, sourceType, request.FromUtc, request.ToUtc, request.Page, request.PageSize)), cancellationToken);
        return Results.Ok(new PagedResponse<ContentResponse>(
            result.Items.Select(ContentResponseMapper.Map).ToList(), result.Page, result.PageSize,
            result.TotalCount, result.TotalPages, result.HasPreviousPage, result.HasNextPage));
    }

    private static async Task<IResult> CreateContentAsync(
        CreateContentApiRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        ContentSourceType? sourceType = null;

        if (!string.IsNullOrWhiteSpace(request.SourceType))
        {
            if (!ContentSourceTypeParser.TryParse(request.SourceType, out var parsedSourceType))
            {
                return Results.BadRequest(new
                {
                    error = "Invalid sourceType. Use Manual, Telegram, Instagram, Article, YouTube, Pdf or Image."
                });
            }

            sourceType = parsedSourceType;
        }

        var contentItem = await sender.Send(new CreateContentMediatorRequest(
            new CreateContentCommand(
                Text: request.Text,
                SourceType: sourceType)), cancellationToken);

        return Results.Created($"/api/v1/contents/{contentItem.Id}", ContentResponseMapper.Map(contentItem));
    }

    private static async Task<IResult> GetContentByIdAsync(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var contentItem = await sender.Send(new GetContentMediatorRequest(id), cancellationToken);

        return contentItem is null
            ? Results.NotFound()
            : Results.Ok(ContentResponseMapper.Map(contentItem));
    }

    private static async Task<IResult> GetContentChunksByIdAsync(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var chunks = await sender.Send(new GetContentChunksMediatorRequest(id), cancellationToken);

        return Results.Ok(chunks.Select(chunk => new ContentChunkResponse(
            Id: chunk.Id,
            ContentItemId: chunk.ContentItemId,
            Index: chunk.Index,
            Text: chunk.Text,
            CharStart: chunk.CharStart,
            CharEnd: chunk.CharEnd,
            HasEmbedding: chunk.Embedding is not null,
            CreatedAtUtc: chunk.CreatedAtUtc)));
    }
}
