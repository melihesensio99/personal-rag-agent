using TelegramAi.Backend.Api.Contracts.Telegram;
using TelegramAi.Backend.Api.Mappers;
using TelegramAi.Backend.Application.Telegram.Commands;
using TelegramAi.Backend.Application.Telegram.Services;

namespace TelegramAi.Backend.Api;

public static class TelegramEndpoints
{
    public static IEndpointRouteBuilder MapTelegramEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/telegram/test-messages", SimulateMessageAsync);

        return endpoints;
    }

    private static async Task<IResult> SimulateMessageAsync(
        SimulateTelegramMessageRequest request,
        ITelegramMessageApplicationService telegramMessageApplicationService,
        CancellationToken cancellationToken)
    {
        var result = await telegramMessageApplicationService.ProcessAsync(
            new ProcessTelegramMessageCommand(
                ChatId: request.ChatId,
                Text: request.Text,
                SenderDisplayName: request.SenderDisplayName),
            cancellationToken);

        return Results.Ok(TelegramMessageProcessingResponseMapper.Map(result));
    }
}
