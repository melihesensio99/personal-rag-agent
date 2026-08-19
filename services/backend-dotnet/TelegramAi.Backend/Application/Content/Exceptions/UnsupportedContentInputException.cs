namespace TelegramAi.Backend.Application.Content.Exceptions;

public sealed class UnsupportedContentInputException(string userMessage) : Exception(userMessage)
{
    public string UserMessage { get; } = userMessage;
}
