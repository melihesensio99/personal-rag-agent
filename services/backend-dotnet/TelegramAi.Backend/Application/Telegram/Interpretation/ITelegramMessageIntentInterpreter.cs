namespace TelegramAi.Backend.Application.Telegram.Interpretation;

public interface ITelegramMessageIntentInterpreter
{
    TelegramMessageIntent Interpret(string messageText);
}
