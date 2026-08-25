using Microsoft.Extensions.Options;
using TelegramAi.Backend.Application.Content.Exceptions;
using TelegramAi.Backend.Application.Content.Services;
using TelegramAi.Backend.Application.Telegram.Commands;
using TelegramAi.Backend.Application.Telegram.Exceptions;
using TelegramAi.Backend.Application.Telegram.Formatting;
using TelegramAi.Backend.Application.Telegram.Interpretation;
using TelegramAi.Backend.Application.Telegram.Services;
using TelegramAi.Backend.Domain.Content;
using TelegramAi.Backend.Infrastructure.AiService;
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
        var contentApplicationService = scope.ServiceProvider.GetRequiredService<IContentApplicationService>();
        var telegramMessageApplicationService = scope.ServiceProvider.GetRequiredService<ITelegramMessageApplicationService>();
        var aiServiceClient = scope.ServiceProvider.GetRequiredService<IAiServiceClient>();
        var searchResponseFormatter = scope.ServiceProvider.GetRequiredService<ITelegramContentSearchResponseFormatter>();
        var semanticAnswerResponseFormatter = scope.ServiceProvider.GetRequiredService<ITelegramSemanticAnswerResponseFormatter>();
        var responseFormatter = scope.ServiceProvider.GetRequiredService<ITelegramMessageResponseFormatter>();

        try
        {
            var intent = await ResolveIntentAsync(text, aiServiceClient, cancellationToken);

            if (intent is SearchContentsIntent searchIntent)
            {
                if (ShouldUseSemanticAnswer(text, searchIntent.Query))
                {
                    var semanticAnswer = await contentApplicationService.SemanticAnswerAsync(
                        text,
                        5,
                        null,
                        cancellationToken);

                    await telegramBotApiClient.SendTextMessageAsync(
                        message.Chat.Id,
                        semanticAnswerResponseFormatter.Format(semanticAnswer),
                        cancellationToken);
                }
                else
                {
                    var contents = await contentApplicationService.SearchAsync(
                        searchIntent.Query,
                        cancellationToken);

                    await telegramBotApiClient.SendTextMessageAsync(
                        message.Chat.Id,
                        searchResponseFormatter.Format(searchIntent.Query, contents),
                        cancellationToken);
                }

                return;
            }

            if (intent is ClarifyContentIntent)
            {
                await telegramBotApiClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "Bunu kaydetmemi mi yoksa eski kayitlarda aramamı mi istiyorsun? Biraz daha net yazar misin?",
                    cancellationToken);

                return;
            }

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
        catch (UnsupportedContentInputException exception)
        {
            await telegramBotApiClient.SendTextMessageAsync(
                message.Chat.Id,
                exception.UserMessage,
                cancellationToken);
        }
        catch (AiIntentUnavailableException exception)
        {
            logger.LogWarning(exception, "AI intent service failed for chat {ChatId}.", message.Chat.Id);

            await telegramBotApiClient.SendTextMessageAsync(
                message.Chat.Id,
                "AI karar servisi su an gec cevap verdi veya hata aldi. Yanlis islem yapmamak icin bu mesaji islemedim. Birazdan tekrar dener misin?",
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Telegram message processing failed for chat {ChatId}.", message.Chat.Id);

            await telegramBotApiClient.SendTextMessageAsync(
                message.Chat.Id,
                "Bu mesaji islerken bir hata olustu. Logu kontrol edip tekrar deneyelim.",
                cancellationToken);
        }
    }

    private static async Task<TelegramMessageIntent> ResolveIntentAsync(
        string text,
        IAiServiceClient aiServiceClient,
        CancellationToken cancellationToken)
    {
        Api.Contracts.Intents.ClassifyIntentResponse aiIntent;

        try
        {
            aiIntent = await aiServiceClient.ClassifyIntentAsync(
                new Api.Contracts.Intents.ClassifyIntentRequest(
                    Message: text,
                    CurrentDate: ResolveTodayInTurkey().ToString("yyyy-MM-dd")),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new AiIntentUnavailableException(exception);
        }

        if (aiIntent.Intent.Equals("search", StringComparison.OrdinalIgnoreCase))
        {
            var contentKind = ParseContentKind(aiIntent.ContentKind);
            var sourceType = ParseSourceType(aiIntent.SourceType);
            var (fromUtc, toUtc) = ParseTimeFilter(aiIntent.TimeFilter);

            return new SearchContentsIntent(
                text,
                new Application.Content.Queries.SearchContentsQuery(
                    Keywords: aiIntent.Keywords,
                    ContentKind: contentKind,
                    SourceType: sourceType,
                    FromUtc: fromUtc,
                    ToUtc: toUtc));
        }

        if (aiIntent.Intent.Equals("clarify", StringComparison.OrdinalIgnoreCase) ||
            aiIntent.NeedsClarification)
        {
            return new ClarifyContentIntent(text);
        }

        return new SaveContentIntent(text);
    }

    private static ContentSourceType? ParseSourceType(string? sourceType)
    {
        return Enum.TryParse<ContentSourceType>(sourceType, ignoreCase: true, out var parsed)
            ? parsed
            : null;
    }

    private static ContentKind? ParseContentKind(string? contentKind)
    {
        return Enum.TryParse<ContentKind>(contentKind, ignoreCase: true, out var parsed)
            ? parsed
            : null;
    }

    private static (DateTimeOffset? FromUtc, DateTimeOffset? ToUtc) ParseTimeFilter(string timeFilter)
    {
        var now = DateTimeOffset.UtcNow;
        var localNow = TimeZoneInfo.ConvertTime(now, ResolveTurkeyTimeZone());

        return timeFilter switch
        {
            "today" => BuildDayRange(localNow),
            "yesterday" => BuildDayRange(localNow.AddDays(-1)),
            "two_days_ago" => BuildDayRange(localNow.AddDays(-2)),
            _ => (null, null),
        };
    }

    private static (DateTimeOffset FromUtc, DateTimeOffset ToUtc) BuildDayRange(DateTimeOffset localDateTime)
    {
        var timeZone = ResolveTurkeyTimeZone();
        var dayStart = localDateTime.Date;
        var dayEnd = dayStart.AddDays(1);

        return (
            TimeZoneInfo.ConvertTimeToUtc(dayStart, timeZone),
            TimeZoneInfo.ConvertTimeToUtc(dayEnd, timeZone));
    }

    private static bool ShouldUseSemanticAnswer(string originalText, Application.Content.Queries.SearchContentsQuery query)
    {
        var normalized = originalText.Trim().ToLowerInvariant();
        var listVerbs = new[]
        {
            "listele",
            "liste",
            "getir",
            "göster",
            "goster",
            "bul",
            "ara",
            "show",
            "list",
            "find",
        };

        if (listVerbs.Any(verb => normalized.Contains(verb, StringComparison.Ordinal)))
        {
            return false;
        }

        var questionSignals = new[]
        {
            "?",
            "nedir",
            "neden",
            "niçin",
            "nasil",
            "nasıl",
            "ne kadar",
            "kaç",
            "hangi",
            "hangisi",
            "ne diyor",
            "ne söylüyor",
            "ne öneriyor",
            "öneriyor mu",
        };

        return questionSignals.Any(signal => normalized.Contains(signal, StringComparison.Ordinal));
    }

    private static TimeZoneInfo ResolveTurkeyTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        }
    }

    private static DateOnly ResolveTodayInTurkey()
    {
        var localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, ResolveTurkeyTimeZone());
        return DateOnly.FromDateTime(localNow.DateTime);
    }
}
