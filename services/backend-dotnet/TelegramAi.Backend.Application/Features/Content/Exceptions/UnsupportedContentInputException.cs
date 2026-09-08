namespace TelegramAi.Backend.Application.Features.Content.Exceptions;

public sealed class UnsupportedContentInputException(string userMessage) : Exception(userMessage)
{
    public string UserMessage { get; } = userMessage;
}
