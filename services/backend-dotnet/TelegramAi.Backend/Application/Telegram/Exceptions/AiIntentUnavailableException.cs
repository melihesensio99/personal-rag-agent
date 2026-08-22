namespace TelegramAi.Backend.Application.Telegram.Exceptions;

public sealed class AiIntentUnavailableException(Exception innerException)
    : Exception("AI intent service is unavailable.", innerException);
