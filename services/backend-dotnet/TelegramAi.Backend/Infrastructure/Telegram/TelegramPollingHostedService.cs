using Microsoft.Extensions.Options;
using TelegramAi.Backend.Api.Contracts.Intents;
using TelegramAi.Backend.Application.Content.Exceptions;
using TelegramAi.Backend.Application.Content.Queries;
using TelegramAi.Backend.Application.Content.Services;
using TelegramAi.Backend.Application.Telegram.Commands;
using TelegramAi.Backend.Application.Telegram.Exceptions;
using TelegramAi.Backend.Application.Telegram.Formatting;
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
            var decision = await ResolveAgentDecisionAsync(text, aiServiceClient, cancellationToken);

            if (decision.Action.Equals("list_contents", StringComparison.OrdinalIgnoreCase))
            {
                var query = BuildSearchQuery(decision);
                var contents = await contentApplicationService.SearchAsync(
                    query,
                    cancellationToken);

                await telegramBotApiClient.SendTextMessageAsync(
                    message.Chat.Id,
                    searchResponseFormatter.Format(query, contents),
                    cancellationToken);

                return;
            }

            if (decision.Action.Equals("answer_from_memory", StringComparison.OrdinalIgnoreCase))
            {
                var semanticAnswer = await contentApplicationService.SemanticAnswerAsync(
                    ResolveQuestionText(decision, text),
                    8,
                    null,
                    cancellationToken);

                await telegramBotApiClient.SendTextMessageAsync(
                    message.Chat.Id,
                    semanticAnswerResponseFormatter.Format(semanticAnswer),
                    cancellationToken);

                return;
            }

            if (decision.Action.Equals("ask_clarification", StringComparison.OrdinalIgnoreCase) ||
                decision.NeedsClarification)
            {
                await telegramBotApiClient.SendTextMessageAsync(
                    message.Chat.Id,
                    ResolveClarificationMessage(decision),
                    cancellationToken);

                return;
            }

            var result = await telegramMessageApplicationService.ProcessAsync(
                new ProcessTelegramMessageCommand(
                    ChatId: message.Chat.Id,
                    Text: ResolveContentText(decision, text),
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

    private static async Task<ClassifyIntentResponse> ResolveAgentDecisionAsync(
        string text,
        IAiServiceClient aiServiceClient,
        CancellationToken cancellationToken)
    {
        try
        {
            return await aiServiceClient.ClassifyIntentAsync(
                new ClassifyIntentRequest(
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
    }

    private static SearchContentsQuery BuildSearchQuery(ClassifyIntentResponse decision)
    {
        var contentKind = ParseContentKind(decision.ContentKind);
        var sourceType = ParseSourceType(decision.SourceType);
        var (fromUtc, toUtc) = ParseTimeFilter(decision.TimeFilter);

        return new SearchContentsQuery(
            Keywords: decision.Keywords,
            ContentKind: contentKind,
            SourceType: sourceType,
            FromUtc: fromUtc,
            ToUtc: toUtc);
    }

    private static string ResolveQuestionText(ClassifyIntentResponse decision, string fallbackText)
    {
        return string.IsNullOrWhiteSpace(decision.Query)
            ? fallbackText
            : decision.Query.Trim();
    }

    private static string ResolveContentText(ClassifyIntentResponse decision, string fallbackText)
    {
        return string.IsNullOrWhiteSpace(decision.Content)
            ? fallbackText
            : decision.Content.Trim();
    }

    private static string ResolveClarificationMessage(ClassifyIntentResponse decision)
    {
        return string.IsNullOrWhiteSpace(decision.ClarificationMessage)
            ? "Bunu kaydetmemi mi yoksa eski kayitlarda aramamı mi istiyorsun? Biraz daha net yazar misin?"
            : decision.ClarificationMessage.Trim();
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
