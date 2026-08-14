namespace TelegramAi.Backend.Infrastructure.Telegram;

public sealed class TelegramBotOptions
{
    public const string SectionName = "TelegramBot";

    public bool Enabled { get; init; }
    public string Mode { get; init; } = "Polling";
    public string BotToken { get; init; } = string.Empty;
    public string PublicUsername { get; init; } = string.Empty;

    public bool HasToken => !string.IsNullOrWhiteSpace(BotToken);
}
