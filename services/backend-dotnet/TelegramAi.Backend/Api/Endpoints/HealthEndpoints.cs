using TelegramAi.Backend.Api.Contracts.Health;
using TelegramAi.Backend.Infrastructure.AiService;

namespace TelegramAi.Backend.Api;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", () => Results.Ok(
            new BackendHealthResponse(
                Service: "backend-dotnet",
                Status: "healthy",
                Version: "1.0")));

        endpoints.MapGet("/api/v1/system/health", GetSystemHealthAsync);

        return endpoints;
    }

    private static async Task<IResult> GetSystemHealthAsync(
        IAiServiceClient aiServiceClient,
        CancellationToken cancellationToken)
    {
        var aiHealth = await aiServiceClient.GetHealthAsync(cancellationToken);

        var response = new BackendSystemHealthResponse(
            Service: "backend-dotnet",
            Status: "healthy",
            Dependencies: new BackendDependencyHealthResponse(aiHealth));

        return Results.Ok(response);
    }
}
