namespace TelegramAi.Backend.Api.Contracts.Intents;

public sealed record ClassifyIntentRequest(
    string Message,
    string CurrentDate);
