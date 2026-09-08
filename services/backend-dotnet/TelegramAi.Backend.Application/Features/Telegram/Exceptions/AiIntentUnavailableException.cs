namespace TelegramAi.Backend.Application.Features.Telegram.Exceptions;

public sealed class AiIntentUnavailableException(Exception innerException)
    : Exception("AI intent service is unavailable.", innerException);
