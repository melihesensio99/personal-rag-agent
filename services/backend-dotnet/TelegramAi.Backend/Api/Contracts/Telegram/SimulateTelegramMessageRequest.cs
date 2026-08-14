namespace TelegramAi.Backend.Api.Contracts.Telegram;

public sealed record SimulateTelegramMessageRequest(
    long ChatId,
    string Text,
    string? SenderDisplayName);
