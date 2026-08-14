using Microsoft.Extensions.Options;
using TelegramAi.Backend.Application.Telegram.Commands;
using TelegramAi.Backend.Application.Telegram.Formatting;
using TelegramAi.Backend.Application.Telegram.Services;
using TelegramAi.Backend.Infrastructure.Telegram.TelegramApi;

namespace TelegramAi.Backend.Infrastructure.Telegram;

public sealed class TelegramPollingHostedService(
    IServiceScopeFactory serviceScopeFactory,
    ITelegramBotApiClient telegramBotApiClient,
    IOptions<TelegramBotOptions> options,
    ILogger<TelegramPollingHostedService> logger) : BackgroundService
{
    private readonly TelegramBotOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || !_options.HasToken || !_options.Mode.Equals("Polling", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        logger.LogInformation("Telegram polling started for @{PublicUsername}.", _options.PublicUsername);

        long? offset = null;

        if (_options.DropPendingUpdatesOnStartup)
        {
            var pendingUpdates = await telegramBotApiClient.GetUpdatesAsync(
                offset: -1,
                timeoutSeconds: 0,
                stoppingToken);

            var lastPendingUpdate = pendingUpdates.LastOrDefault();
            if (lastPendingUpdate is not null)
            {
                offset = lastPendingUpdate.UpdateId + 1;
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var updates = await telegramBotApiClient.GetUpdatesAsync(
                    offset,
                    _options.PollingTimeoutSeconds,
                    stoppingToken);

                foreach (var update in updates)
                {
                    offset = update.UpdateId + 1;
                    await ProcessUpdateAsync(update, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Telegram polling iteration failed.");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }

    private async Task ProcessUpdateAsync(
        TelegramUpdate update,
        CancellationToken cancellationToken)
    {
        var message = update.Message;
        var text = message?.Text?.Trim();

        if (message is null || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
        {
            await telegramBotApiClient.SendTextMessageAsync(
                message.Chat.Id,
                "Merhaba. Bana bir metin veya link aciklamasi gonder, ben onu kaydedip ozetleyeyim.",
                cancellationToken);

            return;
        }

        using var scope = serviceScopeFactory.CreateScope();
        var telegramMessageApplicationService = scope.ServiceProvider.GetRequiredService<ITelegramMessageApplicationService>();
        var responseFormatter = scope.ServiceProvider.GetRequiredService<ITelegramMessageResponseFormatter>();

        var result = await telegramMessageApplicationService.ProcessAsync(
            new ProcessTelegramMessageCommand(
                ChatId: message.Chat.Id,
                Text: text,
                SenderDisplayName: message.From?.FirstName ?? message.From?.Username),
            cancellationToken);

        await telegramBotApiClient.SendTextMessageAsync(
            message.Chat.Id,
            responseFormatter.Format(result),
            cancellationToken);
    }
}
