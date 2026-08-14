using TelegramAi.Backend.Api.Contracts.Content;

namespace TelegramAi.Backend.Api.Contracts.Telegram;

public sealed record TelegramMessageProcessingResponse(
    long ChatId,
    string SenderDisplayName,
    DateTimeOffset ReceivedAtUtc,
    ContentResponse Content);
