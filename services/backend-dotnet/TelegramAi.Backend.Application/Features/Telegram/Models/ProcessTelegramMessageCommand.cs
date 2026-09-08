namespace TelegramAi.Backend.Application.Features.Telegram.Commands;

public sealed record ProcessTelegramMessageCommand(
    long ChatId,
    string Text,
    string? SenderDisplayName);
