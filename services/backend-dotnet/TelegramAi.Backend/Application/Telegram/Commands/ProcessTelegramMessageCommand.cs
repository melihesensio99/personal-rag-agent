namespace TelegramAi.Backend.Application.Telegram.Commands;

public sealed record ProcessTelegramMessageCommand(
    long ChatId,
    string Text,
    string? SenderDisplayName);
