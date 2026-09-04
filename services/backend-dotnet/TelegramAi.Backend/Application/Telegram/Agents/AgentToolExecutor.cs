using TelegramAi.Backend.Api.Contracts.Intents;
using TelegramAi.Backend.Application.Content.Queries;
using TelegramAi.Backend.Application.Content.Services;
using TelegramAi.Backend.Application.Telegram.Commands;
using TelegramAi.Backend.Application.Telegram.Formatting;
using TelegramAi.Backend.Application.Telegram.Services;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Telegram.Agents;

public sealed class AgentToolExecutor(
    IContentApplicationService contentApplicationService,
    ITelegramMessageApplicationService telegramMessageApplicationService,
    ITelegramContentSearchResponseFormatter searchFormatter,
    ITelegramSemanticAnswerResponseFormatter answerFormatter,
    ITelegramMessageResponseFormatter messageFormatter) : IAgentToolExecutor
{
    private static readonly HashSet<string> InstructionWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "makale", "makaleler", "makaleleri", "article", "articles", "video", "videolar", "videoları", "videolari",
        "youtube", "link", "linkleri", "kayıt", "kayıtları", "kayit", "kayitlari", "getir", "listele", "bul",
        "göster", "goster", "bugün", "bugun", "dün", "dun", "attığım", "attigim"
    };

    public async Task<IReadOnlyList<string>> ExecuteAsync(AgentPlan plan, long chatId, string fallbackText, string? senderDisplayName, CancellationToken cancellationToken)
    {
        return plan.Tool switch
        {
            AgentTool.SearchSavedContent => await ExecuteSearchSavedContentAsync(plan.Decision, cancellationToken),
            AgentTool.AnswerUsingSavedContent => await ExecuteAnswerUsingSavedContentAsync(plan.Decision, fallbackText, cancellationToken),
            AgentTool.AskUserForClarification => [string.IsNullOrWhiteSpace(plan.Decision.ClarificationMessage) ? "Bunu kaydetmemi mi yoksa eski kayitlarda aramamı mi istiyorsun?" : plan.Decision.ClarificationMessage.Trim()],
            _ => await ExecuteSaveIncomingContentAsync(chatId, fallbackText, senderDisplayName, plan.Decision, cancellationToken)
        };
    }

    private async Task<IReadOnlyList<string>> ExecuteSearchSavedContentAsync(ClassifyIntentResponse decision, CancellationToken cancellationToken)
    {
        var (fromUtc, toUtc) = ResolveDateRange(decision);
        var query = new SearchContentsQuery(
            decision.Keywords.Where(x => !string.IsNullOrWhiteSpace(x) && !InstructionWords.Contains(x.Trim())).Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToArray(),
            Enum.TryParse<ContentKind>(decision.ContentKind, true, out var kind) ? kind : null,
            Enum.TryParse<ContentSourceType>(decision.SourceType, true, out var source) ? source : null,
            fromUtc, toUtc, decision.SemanticQuery);
        var contents = await contentApplicationService.SearchAsync(query, cancellationToken);
        return searchFormatter.FormatMessages(query, contents);
    }

    private async Task<IReadOnlyList<string>> ExecuteAnswerUsingSavedContentAsync(ClassifyIntentResponse decision, string fallbackText, CancellationToken cancellationToken)
    {
        var question = string.IsNullOrWhiteSpace(decision.Query) ? fallbackText : decision.Query.Trim();
        var result = await contentApplicationService.SemanticAnswerAsync(question, 8, null, cancellationToken, decision.SemanticQuery);
        var messages = new List<string> { answerFormatter.Format(result) };
        messages.AddRange(answerFormatter.FormatSourceMessages(result));
        return messages;
    }

    private async Task<IReadOnlyList<string>> ExecuteSaveIncomingContentAsync(long chatId, string fallbackText, string? senderDisplayName, ClassifyIntentResponse decision, CancellationToken cancellationToken)
    {
        // For URL messages, always preserve the original URL. The intent model may
        // return a preview/summary in `content`, which would prevent extraction.
        var contentToSave = ContainsUrl(fallbackText)
            ? fallbackText.Trim()
            : string.IsNullOrWhiteSpace(decision.Content) ? fallbackText : decision.Content.Trim();
        var result = await telegramMessageApplicationService.ProcessAsync(new ProcessTelegramMessageCommand(chatId, contentToSave, senderDisplayName), cancellationToken);
        return [messageFormatter.Format(result)];
    }

    private static bool ContainsUrl(string text)
    {
        return text.Contains("http://", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("https://", StringComparison.OrdinalIgnoreCase);
    }

    private static DateTimeOffset? ParseDate(string? value)
    {
        if (!DateTimeOffset.TryParse(value, out var parsed)) return null;
        return parsed.Date;
    }

    private static (DateTimeOffset? FromUtc, DateTimeOffset? ToUtc) ResolveDateRange(ClassifyIntentResponse decision)
    {
        var explicitFrom = ParseDate(decision.DateFrom);
        var explicitTo = ParseDate(decision.DateTo);
        if (explicitFrom.HasValue || explicitTo.HasValue)
        {
            return (explicitFrom, explicitTo);
        }

        var todayUtc = DateTimeOffset.UtcNow.Date;
        return decision.TimeFilter.ToLowerInvariant() switch
        {
            "today" => (todayUtc, todayUtc.AddDays(1)),
            "yesterday" => (todayUtc.AddDays(-1), todayUtc),
            "two_days_ago" => (todayUtc.AddDays(-2), todayUtc.AddDays(-1)),
            _ => (null, null),
        };
    }
}
