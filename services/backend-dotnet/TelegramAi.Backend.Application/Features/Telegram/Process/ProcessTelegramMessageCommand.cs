namespace TelegramAi.Backend.Application.Features.Telegram.Process;

public sealed record ProcessTelegramMessageCommand(
    long ChatId,
    string Text,
    string? SenderDisplayName);
