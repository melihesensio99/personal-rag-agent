using TelegramAi.Backend.Api.Contracts.Summaries;
using TelegramAi.Backend.Infrastructure.AiService;
using TelegramAi.Backend.Application.Contracts.Summaries;
using TelegramAi.Backend.Application.Shared.Abstractions;
using MediatR;
using CreateSummaryMediatorRequest = TelegramAi.Backend.Application.Features.Content.Summary.CreateSummaryRequest;
using CreateSummaryApiRequest = TelegramAi.Backend.Api.Contracts.Summaries.CreateSummaryRequest;
using TelegramAi.Backend.Api.Validation;

namespace TelegramAi.Backend.Api;

public static class SummaryEndpoints
{
    public static IEndpointRouteBuilder MapSummaryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/content/summaries", CreateSummaryAsync)
            .AddEndpointFilter<FluentValidationEndpointFilter<CreateSummaryApiRequest>>();

        return endpoints;
    }

    private static async Task<IResult> CreateSummaryAsync(
        CreateSummaryApiRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(new CreateSummaryMediatorRequest(new CreateSummaryInput(request.ContentId, request.Text)), cancellationToken);
        return Results.Ok(new CreateSummaryResponse(response.ContentId, response.Title, response.ShortSummary, response.KeyPoints, response.Tags, response.Language, response.Provider));
    }
}
