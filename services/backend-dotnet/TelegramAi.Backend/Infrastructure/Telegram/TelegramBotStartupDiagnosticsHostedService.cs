using Microsoft.Extensions.Options;

namespace TelegramAi.Backend.Infrastructure.Telegram;

public sealed class TelegramBotStartupDiagnosticsHostedService(
    IOptions<TelegramBotOptions> options,
    ILogger<TelegramBotStartupDiagnosticsHostedService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var telegramBotOptions = options.Value;

        if (!telegramBotOptions.Enabled)
        {
            logger.LogInformation("Telegram bot integration is disabled.");
            return Task.CompletedTask;
        }

        if (!telegramBotOptions.HasToken)
        {
            logger.LogWarning(
                "Telegram bot integration is enabled but BotToken is empty. Configure TelegramBot:BotToken before starting the real bot.");

            return Task.CompletedTask;
        }

        logger.LogInformation(
            "Telegram bot infrastructure is configured in {Mode} mode for @{PublicUsername}.",
            telegramBotOptions.Mode,
            string.IsNullOrWhiteSpace(telegramBotOptions.PublicUsername)
                ? "unassigned-bot"
                : telegramBotOptions.PublicUsername);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
