using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Telegram.Process;

public sealed record ProcessTelegramMessageResult(
    long ChatId,
    string SenderDisplayName,
    DateTimeOffset ReceivedAtUtc,
    ContentItem Content);
