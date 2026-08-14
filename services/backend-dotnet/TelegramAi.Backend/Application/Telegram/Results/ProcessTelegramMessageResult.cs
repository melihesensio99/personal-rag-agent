using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Telegram.Results;

public sealed record ProcessTelegramMessageResult(
    long ChatId,
    string SenderDisplayName,
    DateTimeOffset ReceivedAtUtc,
    ContentItem Content);
