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

        return endpoints;
    }

    private static async Task<IResult> CreateContentAsync(
        CreateContentRequest request,
        IContentApplicationService contentApplicationService,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ContentSourceType>(request.SourceType, ignoreCase: true, out var sourceType))
        {
            return Results.BadRequest(new
            {
                error = "Invalid sourceType. Use Manual, Telegram, Instagram or Article."
            });
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
}
