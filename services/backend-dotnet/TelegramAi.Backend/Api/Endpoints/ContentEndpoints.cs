using TelegramAi.Backend.Api.Contracts.Content;
using TelegramAi.Backend.Api.Mappers;
using TelegramAi.Backend.Application.Content.Commands;
using TelegramAi.Backend.Application.Content.Services;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Api;

public static class ContentEndpoints
{
    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/contents", CreateContentAsync);
        endpoints.MapGet("/api/v1/contents/{id:guid}", GetContentByIdAsync);
        endpoints.MapGet("/api/v1/contents/{id:guid}/chunks", GetContentChunksByIdAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateContentAsync(
        CreateContentRequest request,
        IContentApplicationService contentApplicationService,
        CancellationToken cancellationToken)
    {
        ContentSourceType? sourceType = null;

        if (!string.IsNullOrWhiteSpace(request.SourceType))
        {
            if (!Enum.TryParse<ContentSourceType>(request.SourceType, ignoreCase: true, out var parsedSourceType))
            {
                return Results.BadRequest(new
                {
                    error = "Invalid sourceType. Use Manual, Telegram, Instagram, Article, YouTube, Pdf or Image."
                });
            }

            sourceType = parsedSourceType;
        }

        var contentItem = await contentApplicationService.CreateAsync(
            new CreateContentCommand(
                Text: request.Text,
                SourceType: sourceType),
            cancellationToken);

        return Results.Created($"/api/v1/contents/{contentItem.Id}", ContentResponseMapper.Map(contentItem));
    }

    private static async Task<IResult> GetContentByIdAsync(
        Guid id,
        IContentApplicationService contentApplicationService,
        CancellationToken cancellationToken)
    {
        var contentItem = await contentApplicationService.GetByIdAsync(id, cancellationToken);

        return contentItem is null
            ? Results.NotFound()
            : Results.Ok(ContentResponseMapper.Map(contentItem));
    }

    private static async Task<IResult> GetContentChunksByIdAsync(
        Guid id,
        IContentApplicationService contentApplicationService,
        CancellationToken cancellationToken)
    {
        var chunks = await contentApplicationService.GetChunksByContentIdAsync(id, cancellationToken);

        return Results.Ok(chunks.Select(chunk => new ContentChunkResponse(
            Id: chunk.Id,
            ContentItemId: chunk.ContentItemId,
            Index: chunk.Index,
            Text: chunk.Text,
            CharStart: chunk.CharStart,
            CharEnd: chunk.CharEnd,
            CreatedAtUtc: chunk.CreatedAtUtc)));
    }
}
