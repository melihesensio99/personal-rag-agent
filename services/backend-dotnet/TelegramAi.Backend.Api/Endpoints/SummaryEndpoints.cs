using TelegramAi.Backend.Api.Contracts.Summaries;
using TelegramAi.Backend.Application.Contracts.Summaries;
using TelegramAi.Backend.Application.Features.Content.Summary;
using TelegramAi.Backend.Application.Shared.Abstractions;
using MediatR;
using TelegramAi.Backend.Api.Validation;

namespace TelegramAi.Backend.Api;

public static class SummaryEndpoints
{
    public static IEndpointRouteBuilder MapSummaryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/content/summaries", CreateSummaryAsync)
            .AddEndpointFilter<FluentValidationEndpointFilter<CreateSummaryRequest>>();

        return endpoints;
    }

    private static async Task<IResult> CreateSummaryAsync(
        CreateSummaryRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(new CreateSummaryCommand(new CreateSummaryInput(request.ContentId, request.Text)), cancellationToken);
        return Results.Ok(new CreateSummaryResponse(response.ContentId, response.Title, response.ShortSummary, response.KeyPoints, response.Tags, response.Language, response.Provider));
    }
}
