using TelegramAi.Backend.Api.Contracts.Summaries;
using TelegramAi.Backend.Infrastructure.AiService;

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
        var response = await aiServiceClient.CreateSummaryAsync(request, cancellationToken);
        return Results.Ok(response);
    }
}
