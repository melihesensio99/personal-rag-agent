using Microsoft.Extensions.Options;
using TelegramAi.Backend.Application.Content.Exceptions;
using TelegramAi.Backend.Application.Telegram.Agents;
using TelegramAi.Backend.Application.Telegram.Exceptions;
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
        if (!_options.Enabled || !_options.HasToken || !_options.Mode.Equals("Polling", StringComparison.OrdinalIgnoreCase)) return;

        logger.LogInformation("Telegram polling started for @{PublicUsername}.", _options.PublicUsername);
        long? offset = null;
        if (_options.DropPendingUpdatesOnStartup)
        {
            var pending = await telegramBotApiClient.GetUpdatesAsync(-1, 0, stoppingToken);
            if (pending.LastOrDefault() is { } last) offset = last.UpdateId + 1;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var updates = await telegramBotApiClient.GetUpdatesAsync(offset, _options.PollingTimeoutSeconds, stoppingToken);
                foreach (var update in updates)
                {
                    offset = update.UpdateId + 1;
                    await ProcessUpdateAsync(update, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                logger.LogError(exception, "Telegram polling iteration failed.");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }

    private async Task ProcessUpdateAsync(TelegramUpdate update, CancellationToken cancellationToken)
    {
        var message = update.Message;
        var text = message?.Text?.Trim();
        if (message is null || string.IsNullOrWhiteSpace(text)) return;

        if (text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
        {
            await telegramBotApiClient.SendTextMessageAsync(message.Chat.Id,
                "Merhaba. Bana bir metin veya link aciklamasi gonder, ben onu kaydedip ozetleyeyim.", cancellationToken);
            return;
        }

        using var scope = serviceScopeFactory.CreateScope();
        var agent = scope.ServiceProvider.GetRequiredService<IAgentOrchestrator>();
        try
        {
            var responses = await agent.ExecuteAsync(message.Chat.Id, text,
                message.From?.FirstName ?? message.From?.Username, cancellationToken);
            foreach (var response in responses)
                await telegramBotApiClient.SendTextMessageAsync(message.Chat.Id, response, cancellationToken);
        }
        catch (UnsupportedContentInputException exception)
        {
            await telegramBotApiClient.SendTextMessageAsync(message.Chat.Id, exception.UserMessage, cancellationToken);
        }
        catch (AiIntentUnavailableException exception)
        {
            logger.LogWarning(exception, "AI intent service failed for chat {ChatId}.", message.Chat.Id);
            await telegramBotApiClient.SendTextMessageAsync(message.Chat.Id,
                "AI karar servisi su an gec cevap verdi veya hata aldi. Birazdan tekrar dener misin?", cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Telegram message processing failed for chat {ChatId}.", message.Chat.Id);
            await telegramBotApiClient.SendTextMessageAsync(message.Chat.Id,
                "Bu mesaji islerken bir hata olustu. Logu kontrol edip tekrar deneyelim.", cancellationToken);
        }
    }
}
