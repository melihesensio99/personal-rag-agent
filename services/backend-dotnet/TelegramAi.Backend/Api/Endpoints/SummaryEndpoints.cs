using TelegramAi.Backend.Api.Contracts.Summaries;
using TelegramAi.Backend.Infrastructure.AiService;
using TelegramAi.Backend.Application.Contracts.Summaries;
using TelegramAi.Backend.Application.Abstractions;

namespace TelegramAi.Backend.Api;

public static class SummaryEndpoints
{
    public static IEndpointRouteBuilder MapSummaryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/content/summaries", CreateSummaryAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateSummaryAsync(
        CreateSummaryRequest request,
        IAiServiceClient aiServiceClient,
        CancellationToken cancellationToken)
    {
        var response = await aiServiceClient.CreateSummaryAsync(new CreateSummaryInput(request.ContentId, request.Text), cancellationToken);
        return Results.Ok(new CreateSummaryResponse(response.ContentId, response.Title, response.ShortSummary, response.KeyPoints, response.Tags, response.Language, response.Provider));
    }
}
