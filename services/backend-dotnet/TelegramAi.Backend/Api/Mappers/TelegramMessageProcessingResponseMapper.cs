using TelegramAi.Backend.Api.Contracts.Telegram;
using TelegramAi.Backend.Application.Telegram.Results;

namespace TelegramAi.Backend.Api.Mappers;

public static class TelegramMessageProcessingResponseMapper
{
    public static TelegramMessageProcessingResponse Map(ProcessTelegramMessageResult result)
    {
        return new TelegramMessageProcessingResponse(
            ChatId: result.ChatId,
            SenderDisplayName: result.SenderDisplayName,
            ReceivedAtUtc: result.ReceivedAtUtc,
            Content: ContentResponseMapper.Map(result.Content));
    }
}
