namespace TelegramAi.Backend.Infrastructure.Telegram;

public sealed class TelegramBotOptions
{
    public const string SectionName = "TelegramBot";

    public bool Enabled { get; init; }
    public string Mode { get; init; } = "Polling";
    public string BotToken { get; init; } = string.Empty;
    public string PublicUsername { get; init; } = string.Empty;
    public int PollingTimeoutSeconds { get; init; } = 10;
    public bool DropPendingUpdatesOnStartup { get; init; } = true;

    public bool HasToken => !string.IsNullOrWhiteSpace(BotToken);
}
